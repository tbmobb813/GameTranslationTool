"""Base adapter interface for all game format handlers."""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass
class TextEntry:
    """Represents a translatable text entry extracted from a game file."""

    id: str
    original: str
    translated: str = ""
    context: Optional[Dict[str, Any]] = field(default=None)
    metadata: Optional[Dict[str, Any]] = field(default=None)

    def to_dict(self) -> Dict[str, Any]:
        """Serialize this entry to a plain dictionary."""
        return {
            "id": self.id,
            "original": self.original,
            "translated": self.translated,
            "context": self.context or {},
            "metadata": self.metadata or {},
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "TextEntry":
        """Deserialize a TextEntry from a plain dictionary."""
        return cls(
            id=data["id"],
            original=data["original"],
            translated=data.get("translated", ""),
            context=data.get("context") or None,
            metadata=data.get("metadata") or None,
        )


class BaseAdapter(ABC):
    """Abstract base class that every game format adapter must implement."""

    @abstractmethod
    def detect(self, file_path: str) -> bool:
        """Return ``True`` if this adapter can handle *file_path*."""

    @abstractmethod
    def extract(self, file_path: str) -> List[TextEntry]:
        """Extract all translatable text entries from *file_path*."""

    @abstractmethod
    def rebuild(
        self,
        file_path: str,
        entries: List[TextEntry],
        output_path: str,
    ) -> bool:
        """Write a new copy of *file_path* at *output_path* with translated text.

        Returns ``True`` on success, ``False`` otherwise.
        """
