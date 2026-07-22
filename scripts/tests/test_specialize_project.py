from __future__ import annotations

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path, PurePosixPath


SCRIPTS = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = SCRIPTS.parent
sys.path.insert(0, str(SCRIPTS))

from project_identity import load_project_identity  # noqa: E402
from specialize_project import (  # noqa: E402
    SpecializationError,
    audit_target,
    derive_identity,
    transform_relative_path,
    transform_text,
    validate_output_path,
)


class IdentityDerivationTests(unittest.TestCase):
    def test_derives_all_supported_identity_forms(self) -> None:
        identity = derive_identity("  Harbor   API  ", "HarborAPI2")

        self.assertEqual("Harbor API", identity.display_name)
        self.assertEqual("HarborAPI2", identity.code_name)
        self.assertEqual("harborAPI2", identity.camel_name)
        self.assertEqual("harbor-api-2", identity.kebab_name)
        self.assertEqual("harbor_api_2", identity.snake_name)
        self.assertEqual("ha", identity.short_prefix)

    def test_rejects_unsafe_or_template_identifiers(self) -> None:
        invalid = (
            ("Harbor", "harbor"),
            ("Harbor", "Harbor-API"),
            ("Harbor Starter", "Harbor"),
            ("Harbor", "HarborStarter"),
            ("Console", "Con"),
        )
        for display_name, code_name in invalid:
            with self.subTest(display_name=display_name, code_name=code_name):
                with self.assertRaises(SpecializationError):
                    derive_identity(display_name, code_name)


class IdentityTransformationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.source = load_project_identity(REPOSITORY_ROOT)
        self.target = derive_identity("Harbor Circle", "HarborCircle")

    def test_rewrites_code_runtime_documentation_and_requirement_forms(self) -> None:
        source = (
            "Community Starter CommunityStarter communityStarter community-starter "
            "community_starter communitystarter cs-root cs_session "
            "L1-STRT-003 L2-STRT-008 starter Starter STARTER SpecializeStarterSafely"
        )

        transformed = transform_text(source, self.source, self.target)

        self.assertIn("Harbor Circle", transformed)
        self.assertIn("HarborCircle harborCircle harbor-circle harbor_circle harborcircle", transformed)
        self.assertIn("hc-root hc_session", transformed)
        self.assertIn("L1-DEVX-003 L2-DEVX-008", transformed)
        self.assertIn("project Project PROJECT SpecializeProjectSafely", transformed)
        self.assertNotIn("starter", transformed.casefold())

    def test_rewrites_identity_bearing_paths(self) -> None:
        path = PurePosixPath(
            "docs/detailed-designs/starter-experience/specialize-starter-safely/"
            "SpecializeStarterSafely.g.cs"
        )

        transformed = transform_relative_path(path, self.source, self.target)

        self.assertEqual(
            PurePosixPath(
                "docs/detailed-designs/developer-experience/specialize-project-safely/"
                "SpecializeProjectSafely.g.cs"
            ),
            transformed,
        )
        self.assertEqual(
            PurePosixPath("security/protect-secrets-diagnostics-data"),
            transform_relative_path(
                PurePosixPath("security/protect-secrets-diagnostics-data"),
                self.source,
                self.target,
            ),
        )

    def test_rejects_existing_nested_and_reserved_destination_paths(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            temporary_root = Path(temporary)
            existing = temporary_root / "existing"
            existing.mkdir()
            with self.assertRaises(SpecializationError):
                validate_output_path(REPOSITORY_ROOT, existing)
            with self.assertRaises(SpecializationError):
                validate_output_path(REPOSITORY_ROOT, REPOSITORY_ROOT / "generated")
            with self.assertRaises(SpecializationError):
                validate_output_path(REPOSITORY_ROOT, temporary_root / "other-starter")


@unittest.skipUnless(
    os.environ.get("RUN_SPECIALIZATION_INTEGRATION") == "1",
    "set RUN_SPECIALIZATION_INTEGRATION=1 to generate and audit a complete target",
)
class FullSpecializationTests(unittest.TestCase):
    def test_generates_clean_harbor_circle_repository(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "harbor-circle"
            subprocess.run(
                [
                    sys.executable,
                    str(SCRIPTS / "specialize_project.py"),
                    "--display-name",
                    "Harbor Circle",
                    "--code-name",
                    "HarborCircle",
                    "--output-path",
                    str(target),
                    "--skip-quality-check",
                ],
                cwd=REPOSITORY_ROOT,
                check=True,
            )
            source = load_project_identity(REPOSITORY_ROOT)
            expected = derive_identity("Harbor Circle", "HarborCircle")
            audit_target(target, source, expected)
            self.assertTrue((target / ".git").is_dir())
            self.assertFalse((target / "eng" / "new-project.ps1").exists())
            self.assertEqual(
                "",
                subprocess.run(
                    ["git", "remote"],
                    cwd=target,
                    check=True,
                    capture_output=True,
                    text=True,
                ).stdout.strip(),
            )
            self.assertIn(
                "No commits yet",
                subprocess.run(
                    ["git", "status", "--short", "--branch"],
                    cwd=target,
                    check=True,
                    capture_output=True,
                    text=True,
                ).stdout,
            )


if __name__ == "__main__":
    unittest.main()
