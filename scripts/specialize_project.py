#!/usr/bin/env python3
"""Create a verified, independently branded project from this repository."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.request
from dataclasses import asdict
from pathlib import Path, PurePosixPath

from project_identity import ProjectIdentity, load_project_identity


PLANTUML_VERSION = "1.2026.4"
PLANTUML_SHA256 = "ebe66f9e67a12dc7aca4e10af67ab29b10ffa281bc72147dbfdc4c00d9de7664"
PLANTUML_URL = (
    "https://github.com/plantuml/plantuml/releases/download/"
    f"v{PLANTUML_VERSION}/plantuml-{PLANTUML_VERSION}.jar"
)

TEMPLATE_ONLY_FILES = {
    PurePosixPath("eng/new-project.ps1"),
    PurePosixPath("scripts/specialize_project.py"),
    PurePosixPath("scripts/tests/__init__.py"),
    PurePosixPath("scripts/tests/test_specialize_project.py"),
}

BINARY_SUFFIXES = {
    ".ico",
    ".jar",
    ".jpg",
    ".jpeg",
    ".pdf",
    ".png",
    ".pyc",
    ".webp",
    ".zip",
}

WINDOWS_RESERVED_NAMES = {
    "aux",
    "clock$",
    "com1",
    "com2",
    "com3",
    "com4",
    "com5",
    "com6",
    "com7",
    "com8",
    "com9",
    "con",
    "lpt1",
    "lpt2",
    "lpt3",
    "lpt4",
    "lpt5",
    "lpt6",
    "lpt7",
    "lpt8",
    "lpt9",
    "nul",
    "prn",
}


class SpecializationError(RuntimeError):
    """A safe, user-actionable specialization failure."""


def split_code_words(code_name: str) -> list[str]:
    return re.findall(r"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+", code_name)


def derive_identity(display_name: str, code_name: str) -> ProjectIdentity:
    display_name = " ".join(display_name.split())
    if not display_name:
        raise SpecializationError("DisplayName cannot be empty.")
    if len(display_name) > 100:
        raise SpecializationError("DisplayName cannot exceed 100 characters.")
    if re.search(r"starter", display_name, re.IGNORECASE):
        raise SpecializationError("DisplayName cannot contain the reserved template identity.")
    if not re.fullmatch(r"[A-Z][A-Za-z0-9]+", code_name):
        raise SpecializationError(
            "CodeName must be a PascalCase identifier containing only ASCII letters and numbers."
        )
    if re.search(r"starter", code_name, re.IGNORECASE):
        raise SpecializationError("CodeName cannot contain the reserved template identity.")
    if code_name.casefold() in WINDOWS_RESERVED_NAMES:
        raise SpecializationError(f"CodeName '{code_name}' is reserved by Windows.")

    words = split_code_words(code_name)
    if not words:
        raise SpecializationError("CodeName does not contain a usable identifier word.")
    kebab_name = "-".join(word.lower() for word in words)
    snake_name = "_".join(word.lower() for word in words)
    initials = "".join(word[0].lower() for word in words if word[0].isalpha())
    letters = "".join(character.lower() for character in code_name if character.isalpha())
    short_prefix = initials[:4] if len(initials) >= 2 else letters[:2]
    if len(short_prefix) < 2:
        raise SpecializationError("CodeName must provide at least two letters for a safe prefix.")

    return ProjectIdentity(
        display_name=display_name,
        code_name=code_name,
        camel_name=code_name[0].lower() + code_name[1:],
        kebab_name=kebab_name,
        snake_name=snake_name,
        short_prefix=short_prefix,
    )


def identity_payload(identity: ProjectIdentity) -> dict[str, str]:
    values = asdict(identity)
    return {
        "displayName": values["display_name"],
        "codeName": values["code_name"],
        "camelName": values["camel_name"],
        "kebabName": values["kebab_name"],
        "snakeName": values["snake_name"],
        "shortPrefix": values["short_prefix"],
    }


def replacement_pairs(source: ProjectIdentity, target: ProjectIdentity) -> list[tuple[str, str]]:
    pairs = [
        ("SpecializeStarterSafely", "SpecializeProjectSafely"),
        ("specialize-starter-safely", "specialize-project-safely"),
        ("specialize starter safely", "specialize project safely"),
        ("Starter adoption and developer experience", "Project development experience"),
        ("starter adoption and developer experience", "project development experience"),
        ("starter-experience", "developer-experience"),
        ("L1-STRT", "L1-DEVX"),
        ("L2-STRT", "L2-DEVX"),
        ("l1-strt", "l1-devx"),
        ("l2-strt", "l2-devx"),
        ("STRT", "DEVX"),
        ("strt", "devx"),
        (source.display_name, target.display_name),
        (source.code_name, target.code_name),
        (source.camel_name, target.camel_name),
        (source.kebab_name, target.kebab_name),
        (source.snake_name, target.snake_name),
        (source.code_name.lower(), target.code_name.lower()),
        ('"prefix": "cs"', f'"prefix": "{target.short_prefix}"'),
    ]
    return sorted(pairs, key=lambda pair: len(pair[0]), reverse=True)


def replace_template_word(text: str) -> str:
    def replacement(match: re.Match[str]) -> str:
        value = match.group(0)
        if value.isupper():
            return "PROJECT"
        if value[0].isupper():
            return "Project"
        return "project"

    return re.sub(r"starter", replacement, text, flags=re.IGNORECASE)


def transform_text(text: str, source: ProjectIdentity, target: ProjectIdentity) -> str:
    for old, new in replacement_pairs(source, target):
        text = text.replace(old, new)
    text = re.sub(
        rf"(?<![A-Za-z0-9]){re.escape(source.short_prefix)}-",
        f"{target.short_prefix}-",
        text,
    )
    text = re.sub(
        rf"(?<![A-Za-z0-9]){re.escape(source.short_prefix)}_",
        f"{target.short_prefix}_",
        text,
    )
    return replace_template_word(text)


def transform_relative_path(
    relative_path: PurePosixPath,
    source: ProjectIdentity,
    target: ProjectIdentity,
) -> PurePosixPath:
    transformed = transform_text(relative_path.as_posix(), source, target)
    return PurePosixPath(transformed)


def git_managed_files(repository_root: Path) -> list[PurePosixPath]:
    result = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        cwd=repository_root,
        check=True,
        capture_output=True,
    )
    return [
        PurePosixPath(value.decode("utf-8"))
        for value in result.stdout.split(b"\0")
        if value
    ]


def is_text_file(path: Path, data: bytes) -> bool:
    if path.suffix.lower() in BINARY_SUFFIXES or b"\0" in data:
        return False
    try:
        data.decode("utf-8-sig")
        return True
    except UnicodeDecodeError:
        return False


def contains_source_identity(data: bytes, source: ProjectIdentity) -> bool:
    try:
        text = data.decode("utf-8-sig")
    except UnicodeDecodeError:
        return False
    markers = (
        source.display_name,
        source.code_name,
        source.camel_name,
        source.kebab_name,
        source.snake_name,
        "STRT",
    )
    return any(marker.casefold() in text.casefold() for marker in markers) or bool(
        re.search(r"starter", text, re.IGNORECASE)
    )


def copy_and_transform(
    source_root: Path,
    staging_root: Path,
    source_identity: ProjectIdentity,
    target_identity: ProjectIdentity,
) -> set[Path]:
    managed_files = git_managed_files(source_root)
    destinations: dict[PurePosixPath, PurePosixPath] = {}
    changed_diagrams: set[Path] = set()

    for relative in managed_files:
        if (
            relative in TEMPLATE_ONLY_FILES
            or "__pycache__" in relative.parts
            or relative.suffix.lower() == ".pyc"
        ):
            continue
        destination = transform_relative_path(relative, source_identity, target_identity)
        if destination in destinations:
            raise SpecializationError(
                f"Identity conversion maps both '{destinations[destination]}' and "
                f"'{relative}' to '{destination}'."
            )
        destinations[destination] = relative

    for destination_relative, source_relative in destinations.items():
        source_path = source_root.joinpath(*source_relative.parts)
        if not source_path.is_file():
            continue
        destination_path = staging_root.joinpath(*destination_relative.parts)
        destination_path.parent.mkdir(parents=True, exist_ok=True)
        data = source_path.read_bytes()
        if source_path.suffix.lower() == ".puml" and contains_source_identity(
            data, source_identity
        ):
            changed_diagrams.add(destination_path)
        if is_text_file(source_path, data):
            text = data.decode("utf-8-sig")
            destination_path.write_text(
                transform_text(text, source_identity, target_identity),
                encoding="utf-8",
                newline="",
            )
        else:
            shutil.copy2(source_path, destination_path)

    return changed_diagrams


def rewrite_root_readme(path: Path, target: ProjectIdentity) -> None:
    text = path.read_text(encoding="utf-8")
    lines = [line for line in text.splitlines() if not line.startswith("[![Quality]")]
    text = "\n".join(lines).rstrip() + "\n"
    text = re.sub(r"^(# .+)\n{3,}", r"\1\n\n", text)
    text = re.sub(
        r"### Create a new project\n.*?(?=### Prerequisites)",
        "",
        text,
        flags=re.DOTALL,
    )
    text = re.sub(
        r"git clone https://github\.com/[^\n]+\nSet-Location [^\n]+\n",
        "Set-Location <repository-path>\n",
        text,
    )
    text = re.sub(
        r"Use the \[issue tracker\]\([^)]+\) for confirmed\n"
        r"defects, scoped feature proposals, and contributor coordination\.",
        "Use the repository's configured issue tracker for confirmed defects, scoped feature "
        "proposals, and contributor coordination.",
        text,
    )
    text = re.sub(
        r"Use GitHub's\n\[private vulnerability reporting\]\([^)]+\)\n"
        r"to share a minimal reproduction and impact assessment without exposing secrets or personal data\.",
        "Use the project's configured private vulnerability reporting channel to share a minimal "
        "reproduction and impact assessment without exposing secrets or personal data.",
        text,
    )
    path.write_text(text, encoding="utf-8", newline="\n")


def write_target_identity(repository_root: Path, identity: ProjectIdentity) -> None:
    path = repository_root / "project.identity.json"
    path.write_text(
        json.dumps(identity_payload(identity), indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def remove_template_commands(repository_root: Path) -> None:
    check_path = repository_root / "eng" / "check.ps1"
    check_lines = [
        line
        for line in check_path.read_text(encoding="utf-8").splitlines()
        if "unittest discover -s scripts/tests" not in line
    ]
    check_path.write_text("\n".join(check_lines).rstrip() + "\n", encoding="utf-8", newline="\n")

    package_path = repository_root / "package.json"
    package = json.loads(package_path.read_text(encoding="utf-8"))
    package["scripts"].pop("test:specialization", None)
    package_path.write_text(
        json.dumps(package, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def run(command: list[str], cwd: Path, label: str) -> None:
    print(f"[{label}] {' '.join(command)}", flush=True)
    try:
        subprocess.run(command, cwd=cwd, check=True)
    except FileNotFoundError as error:
        raise SpecializationError(f"Required command '{command[0]}' is not available.") from error
    except subprocess.CalledProcessError as error:
        raise SpecializationError(f"{label} failed with exit code {error.returncode}.") from error


def regenerate_target(repository_root: Path) -> None:
    run(
        [sys.executable, "scripts/generate_detailed_designs.py"],
        repository_root,
        "requirements and designs",
    )


def format_target_web_files(repository_root: Path) -> None:
    pinned_npm = [
        "npx.cmd" if os.name == "nt" else "npx",
        "--yes",
        "--package",
        "node@24.18.0",
        "--package",
        "npm@11.6.2",
        "npm",
    ]
    run([*pinned_npm, "ci"], repository_root, "web dependency installation")
    run(
        [
            *pinned_npm,
            "exec",
            "prettier",
            "--",
            "--write",
            "{frontend,marketing,design-system}/**/*.{ts,html,scss,css,json,md,astro}",
        ],
        repository_root,
        "specialized web formatting",
    )
    run(
        [sys.executable, "scripts/generate_feature_contracts.py"],
        repository_root,
        "feature contracts",
    )


def plantuml_jar() -> Path:
    cache_root = Path(tempfile.gettempdir()) / "community-project-tools"
    cache_root.mkdir(parents=True, exist_ok=True)
    jar = cache_root / f"plantuml-{PLANTUML_VERSION}.jar"
    if jar.is_file() and hashlib.sha256(jar.read_bytes()).hexdigest() == PLANTUML_SHA256:
        return jar

    download = jar.with_suffix(".download")
    print(f"[diagram renderer] Downloading pinned PlantUML {PLANTUML_VERSION}...", flush=True)
    try:
        with urllib.request.urlopen(PLANTUML_URL, timeout=120) as response:
            with download.open("wb") as stream:
                shutil.copyfileobj(response, stream)
    except (OSError, TimeoutError) as error:
        download.unlink(missing_ok=True)
        raise SpecializationError(f"Unable to download the pinned diagram renderer: {error}") from error
    digest = hashlib.sha256(download.read_bytes()).hexdigest()
    if digest != PLANTUML_SHA256:
        download.unlink(missing_ok=True)
        raise SpecializationError(
            f"PlantUML checksum mismatch: expected {PLANTUML_SHA256}, received {digest}."
        )
    download.replace(jar)
    return jar


def render_diagrams(repository_root: Path, diagram_paths: set[Path]) -> None:
    if not diagram_paths:
        return
    jar = plantuml_jar()
    relative_paths = sorted(path.relative_to(repository_root) for path in diagram_paths)
    for source in relative_paths:
        source.with_suffix(".png")
        (repository_root / source.with_suffix(".png")).unlink(missing_ok=True)
    for offset in range(0, len(relative_paths), 20):
        batch = relative_paths[offset : offset + 20]
        run(
            ["java", "-jar", str(jar), "-tpng", "-nometadata", *map(str, batch)],
            repository_root,
            "diagram rendering",
        )
    missing = [
        path.with_suffix(".png")
        for path in relative_paths
        if not (repository_root / path.with_suffix(".png")).is_file()
    ]
    if missing:
        raise SpecializationError(f"Diagram renderer did not create: {missing}")


def audit_target(
    repository_root: Path,
    source_identity: ProjectIdentity,
    target_identity: ProjectIdentity,
) -> None:
    errors: list[str] = []
    forbidden_values = {
        source_identity.display_name.casefold(),
        source_identity.code_name.casefold(),
        source_identity.camel_name.casefold(),
        source_identity.kebab_name.casefold(),
        source_identity.snake_name.casefold(),
        "strt",
        "starter",
    }
    for path in repository_root.rglob("*"):
        relative = path.relative_to(repository_root)
        relative_text = relative.as_posix().casefold()
        if any(value in relative_text for value in forbidden_values):
            errors.append(f"prohibited identity in path: {relative}")
        if not path.is_file() or path.suffix.lower() in BINARY_SUFFIXES:
            continue
        data = path.read_bytes()
        if not is_text_file(path, data):
            continue
        text = data.decode("utf-8-sig").casefold()
        if any(value in text for value in forbidden_values):
            errors.append(f"prohibited identity in content: {relative}")

    expected = [
        repository_root / "backend" / f"{target_identity.code_name}.sln",
        repository_root
        / "backend"
        / "src"
        / f"{target_identity.code_name}.Api"
        / f"{target_identity.code_name}.Api.csproj",
        repository_root / "docs" / "specs" / "developer-experience" / "L1.md",
        repository_root / "project.identity.json",
    ]
    for path in expected:
        if not path.is_file():
            errors.append(f"expected specialized file is missing: {path.relative_to(repository_root)}")
    for relative in TEMPLATE_ONLY_FILES:
        if repository_root.joinpath(*relative.parts).exists():
            errors.append(f"template-only file remains: {relative}")

    actual_identity = load_project_identity(repository_root)
    if actual_identity != target_identity:
        errors.append("project.identity.json does not match the requested identity")
    if errors:
        detail = "\n".join(f"- {error}" for error in errors[:50])
        raise SpecializationError(f"Target identity audit failed:\n{detail}")


def remove_build_outputs(repository_root: Path) -> None:
    removable_directories = {
        ".angular",
        ".astro",
        "TestResults",
        "artifacts",
        "bin",
        "coverage",
        "dist",
        "node_modules",
        "obj",
        "test-results",
    }
    resolved_root = repository_root.resolve()
    candidates = [
        path
        for path in repository_root.rglob("*")
        if path.is_dir() and path.name in removable_directories
    ]
    for path in sorted(candidates, key=lambda item: len(item.parts), reverse=True):
        resolved = path.resolve()
        if resolved_root not in resolved.parents:
            raise SpecializationError(f"Refusing to clean path outside staging root: {resolved}")
        if path.exists():
            shutil.rmtree(path)


def initialize_git_repository(repository_root: Path) -> None:
    run(["git", "init", "-b", "main"], repository_root, "git initialization")
    run(["git", "add", "--all"], repository_root, "git staging")


def validate_output_path(source_root: Path, output_path: Path) -> Path:
    source_root = source_root.resolve()
    output_path = output_path.resolve()
    if output_path.exists():
        raise SpecializationError(f"OutputPath already exists: {output_path}")
    if source_root == output_path or source_root in output_path.parents:
        raise SpecializationError("OutputPath must be outside the template repository.")
    if re.search(r"starter", output_path.name, re.IGNORECASE):
        raise SpecializationError("OutputPath name cannot contain the reserved template identity.")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    return output_path


def specialize(
    source_root: Path,
    output_path: Path,
    target_identity: ProjectIdentity,
    skip_quality_check: bool = False,
) -> None:
    source_root = source_root.resolve()
    source_identity = load_project_identity(source_root)
    output_path = validate_output_path(source_root, output_path)
    staging_root = Path(
        tempfile.mkdtemp(prefix=f".{output_path.name}.staging-", dir=output_path.parent)
    )
    print(f"[specialization] Building {target_identity.display_name} in {staging_root}", flush=True)
    try:
        changed_diagrams = copy_and_transform(
            source_root, staging_root, source_identity, target_identity
        )
        write_target_identity(staging_root, target_identity)
        rewrite_root_readme(staging_root / "README.md", target_identity)
        remove_template_commands(staging_root)
        regenerate_target(staging_root)
        render_diagrams(staging_root, changed_diagrams)
        audit_target(staging_root, source_identity, target_identity)
        run(
            [sys.executable, "scripts/verify_detailed_designs.py"],
            staging_root,
            "detailed design audit",
        )
        if not skip_quality_check:
            format_target_web_files(staging_root)
            run(
                ["pwsh", "-NoProfile", "-File", "eng/check.ps1"],
                staging_root,
                "complete quality gate",
            )
        remove_build_outputs(staging_root)
        audit_target(staging_root, source_identity, target_identity)
        initialize_git_repository(staging_root)
        os.replace(staging_root, output_path)
    except BaseException:
        if staging_root.exists():
            resolved_staging = staging_root.resolve()
            if resolved_staging.parent == output_path.parent and resolved_staging.name.startswith(
                f".{output_path.name}.staging-"
            ):
                shutil.rmtree(staging_root)
        raise
    print(f"Created {target_identity.display_name} at {output_path}", flush=True)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--display-name", required=True)
    parser.add_argument("--code-name", required=True)
    parser.add_argument("--output-path", type=Path, required=True)
    parser.add_argument(
        "--skip-quality-check",
        action="store_true",
        help="Skip the full build gate; intended only for transformation test automation.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source_root = Path(__file__).resolve().parents[1]
    try:
        target_identity = derive_identity(args.display_name, args.code_name)
        specialize(
            source_root,
            args.output_path,
            target_identity,
            skip_quality_check=args.skip_quality_check,
        )
        return 0
    except (SpecializationError, subprocess.CalledProcessError) as error:
        print(f"Project creation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
