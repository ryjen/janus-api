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
    in {
      devShells.${system}.default = pkgs.mkShell {
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
