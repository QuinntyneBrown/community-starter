#!/usr/bin/env python3
"""Load the repository's canonical product identity."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class ProjectIdentity:
    display_name: str
    code_name: str
    camel_name: str
    kebab_name: str
    snake_name: str
    short_prefix: str


def load_project_identity(repository_root: Path) -> ProjectIdentity:
    path = repository_root / "project.identity.json"
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
        return ProjectIdentity(
            display_name=payload["displayName"],
            code_name=payload["codeName"],
            camel_name=payload["camelName"],
            kebab_name=payload["kebabName"],
            snake_name=payload["snakeName"],
            short_prefix=payload["shortPrefix"],
        )
    except (OSError, KeyError, TypeError, json.JSONDecodeError) as error:
        raise SystemExit(f"Invalid project identity at {path}: {error}") from error
