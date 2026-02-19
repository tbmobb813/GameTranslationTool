"""Text object representation used throughout the pipeline."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Dict, Iterator, List, Optional

from adapters.base import TextEntry


@dataclass
class TextNode:
    """A node in the translation graph wrapping a single :class:`TextEntry`."""

    entry: TextEntry
    children: List["TextNode"] = field(default_factory=list)
    parent: Optional["TextNode"] = field(default=None, repr=False, compare=False)

    @property
    def id(self) -> str:  # noqa: A003
        """Shortcut to the underlying entry id."""
        return self.entry.id

    def add_child(self, node: "TextNode") -> None:
        """Attach *node* as a child of this node."""
        node.parent = self
        self.children.append(node)


class TextGraph:
    """A directed acyclic graph of :class:`TextNode` objects.

    The graph represents the hierarchy of translatable text entries as they
    appear in the source asset (e.g. a scene containing multiple dialogue
    objects each with multiple text fields).
    """

    def __init__(self) -> None:
        self._nodes: Dict[str, TextNode] = {}
        self._roots: List[TextNode] = []

    # ------------------------------------------------------------------
    # Building the graph
    # ------------------------------------------------------------------

    def add_node(self, entry: TextEntry, parent_id: Optional[str] = None) -> TextNode:
        """Add a new node to the graph and return it.

        If *parent_id* is given the new node is attached as a child of the
        existing node with that id.

        Raises:
            KeyError: If *parent_id* is given but does not exist in the graph.
            ValueError: If a node with the same id already exists.
        """
        if entry.id in self._nodes:
            raise ValueError(f"Node with id {entry.id!r} already exists in the graph.")

        node = TextNode(entry=entry)
        self._nodes[entry.id] = node

        if parent_id is not None:
            parent = self._nodes[parent_id]
            parent.add_child(node)
        else:
            self._roots.append(node)

        return node

    # ------------------------------------------------------------------
    # Querying
    # ------------------------------------------------------------------

    def get(self, node_id: str) -> Optional[TextNode]:
        """Return the node with *node_id*, or ``None`` if it doesn't exist."""
        return self._nodes.get(node_id)

    @property
    def roots(self) -> List[TextNode]:
        """Root nodes (nodes without a parent)."""
        return list(self._roots)

    def __len__(self) -> int:
        return len(self._nodes)

    def __iter__(self) -> Iterator[TextNode]:
        """Iterate over all nodes in insertion order."""
        return iter(self._nodes.values())

    # ------------------------------------------------------------------
    # Convenience helpers
    # ------------------------------------------------------------------

    def entries(self) -> List[TextEntry]:
        """Return all :class:`TextEntry` objects in the graph."""
        return [node.entry for node in self._nodes.values()]

    @classmethod
    def from_entries(cls, entries: List[TextEntry]) -> "TextGraph":
        """Build a flat (no hierarchy) graph from a list of entries."""
        graph = cls()
        for entry in entries:
            graph.add_node(entry)
        return graph
