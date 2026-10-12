"""Fail-closed contract tests for the SAM API and IAM boundary.

Requires PyYAML (installed transitively by cfn-lint).
This does not prove CloudFormation deployment or API Gateway runtime behaviour.
"""
from pathlib import Path
import unittest
import yaml

ROOT = Path(__file__).resolve().parents[1]

class CfnLoader(yaml.SafeLoader):
    pass

def intrinsic(loader, tag_suffix, node):
    if isinstance(node, yaml.ScalarNode):
        value = loader.construct_scalar(node)
    elif isinstance(node, yaml.SequenceNode):
        value = loader.construct_sequence(node)
    else:
        value = loader.construct_mapping(node)
    return {f"Fn::{tag_suffix}": value}

CfnLoader.add_multi_constructor("!", intrinsic)

def template():
    return yaml.load((ROOT / "template.yaml").read_text(encoding="utf-8"), Loader=CfnLoader)

def routes(resources):
    output = []
    for function_name, value in resources.items():
        if value.get("Type") != "AWS::Serverless::Function":
            continue
        for event_name, event in value["Properties"].get("Events", {}).items():
            if event.get("Type") != "Api":
                continue
            p = event["Properties"]
            output.append((function_name, event_name, p["Method"].upper(), p["Path"], p.get("Auth", {})))
    return output

class TemplateContractTests(unittest.TestCase):
    def setUp(self):
        self.doc = template()
        self.resources = self.doc["Resources"]
        self.routes = routes(self.resources)

    def test_gateway_default_fail_closed(self):
        auth = self.resources["JanusApi"]["Properties"]["Auth"]
        self.assertEqual(auth["DefaultAuthorizer"], "JanusCognitoAuthorizer")
        self.assertTrue(auth["ApiKeyRequired"])
        self.assertEqual(auth["UsagePlan"]["CreateUsagePlan"], "PER_API")
        self.assertIn("JanusCognitoAuthorizer", auth["Authorizers"])

    def test_auth_routes_are_the_only_authorizer_opt_out(self):
        expected = {("POST", "/signup"), ("POST", "/confirm"), ("POST", "/authenticate"), ("POST", "/reset")}
        actual = set()
        for func, event, method, path, auth in self.routes:
            if auth.get("Authorizer") == "NONE":
                self.assertEqual(func, "AuthFunction")
                self.assertTrue(auth.get("ApiKeyRequired"))
                actual.add((method, path))
        self.assertEqual(actual, expected)

    def test_record_routes_are_limited(self):
        actual = {(method, path) for _,_,method,path,_ in self.routes}
        expected = {
            ("POST", "/signup"), ("POST", "/confirm"), ("POST", "/authenticate"), ("POST", "/reset"),
            ("GET", "/account"), ("GET", "/player"), ("POST", "/player"),
            ("PUT", "/player"), ("DELETE", "/player")
        }
        self.assertEqual(actual, expected)
        for func, _, method, path, auth in self.routes:
            if path in {"/account", "/player"}:
                self.assertNotEqual(auth.get("Authorizer"), "NONE")
                self.assertEqual(auth.get("ApiKeyRequired", True), True)

    def test_no_public_generic_data_function(self):
        self.assertFalse(any(path == "/data" for _,_,_,path,_ in self.routes))
        self.assertNotIn("DataFunction", self.resources)

    def test_record_roles_have_distinct_tables(self):
        for name, table in [("AccountFunction", "AccountTable"),
                            ("AccountProvisionFunction", "AccountTable"),
                            ("PlayerFunction", "PlayerTable")]:
            statements = self.resources[name]["Properties"]["Policies"]
            rendered = repr(statements)
            self.assertIn(table, rendered)
            other = "AccountTable" if table == "PlayerTable" else "PlayerTable"
            self.assertNotIn(other, rendered)

    def test_signup_provisioning_requires_confirmed_cognito_event(self):
        user_pool = self.resources["AccountPool"]["Properties"]
        self.assertTrue(user_pool["AutoVerifiedAttributes"] == ["email"])
        self.assertEqual(user_pool["AdminCreateUserConfig"]["AllowAdminCreateUserOnly"], False)
        self.assertIn("PostConfirmation", user_pool["LambdaConfig"])
        self.assertEqual(self.resources["CognitoConfirmationInvokePermission"]["Type"],
                         "AWS::Lambda::Permission")
        auth_policies = repr(self.resources["AuthFunction"]["Properties"]["Policies"])
        self.assertNotIn("AdminCreateUser", auth_policies)
        self.assertNotIn("sqs:SendMessage", auth_policies)
        confirmed_policies = repr(self.resources["AccountConfirmedFunction"]["Properties"]["Policies"])
        self.assertIn("sqs:SendMessage", confirmed_policies)

    def test_queue_provision_and_dlq(self):
        events = self.resources["AccountProvisionFunction"]["Properties"]["Events"]
        self.assertEqual(events["AccountCreated"]["Type"], "SQS")
        self.assertEqual(events["AccountCreated"]["Properties"]["BatchSize"], 1)
        self.assertIn("RedrivePolicy", self.resources["AccountQueue"]["Properties"])

if __name__ == "__main__":
    unittest.main()
