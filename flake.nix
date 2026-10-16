{
  description = "Playstate source validation (no AWS deployment)";

  # The pinned 26.05 input is shared with ryjen/ops-cadence. Upgrade
  # deliberately, with SAM/.NET compatibility tests and a reviewed lock diff.
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-26.05";

  outputs = { nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = nixpkgs.legacyPackages.${system};
      python = pkgs.python3.withPackages (ps: [ ps.pyyaml ]);
      # These versions are qualified by the last source-only hosted CI.
      # A newer Nixpkgs revision/override needs separate exact-head review.
      qualifiedSamVersion = "1.168.0";
      qualifiedCfnLintVersion = "1.57.2";
    in {
      devShells.${system}.default =
        assert pkgs.lib.assertMsg (pkgs.aws-sam-cli.version == qualifiedSamVersion)
          "SAM version mismatch: pinned Nixpkgs differs from the qualified Playstate SAM CLI; update pin and review.";
        assert pkgs.lib.assertMsg (pkgs.cfn-lint.version == qualifiedCfnLintVersion)
          "cfn-lint version mismatch: pinned Nixpkgs differs from the qualified Playstate linter; update pin and review.";
        pkgs.mkShell {
        packages = [
          pkgs.dotnet-sdk_10
          pkgs.aws-sam-cli
          pkgs.cfn-lint
          pkgs.zip
          pkgs.git
          pkgs.bash
          python
        ];

        DOTNET_CLI_TELEMETRY_OPTOUT = "1";
        SAM_CLI_TELEMETRY = "0";
      };

      # A flake app reuses the exact devShell toolchain. The workflow should
      # invoke: nix develop --command bash ci/validate-source.sh
      # after JIT admission/selector verification, not via hosted Ubuntu.
    };
}
