using System;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings;

namespace dnSpy.Extension.Wasm.TreeView;

[ExportDocumentTabContentFactory(Order = 1)]
internal class HexViewerDocumentTabContentFactory : IDocumentTabContentFactory
{
	private readonly HexEditorFactoryService _editorFactory;
	private readonly HexBufferFactoryService _bufferFactory;

	[ImportingConstructor]
	public HexViewerDocumentTabContentFactory(HexEditorFactoryService editorFactory, HexBufferFactoryService bufferFactory)
	{
		_editorFactory = editorFactory;
		_bufferFactory = bufferFactory;
	}

	public DocumentTabContent? Create(IDocumentTabContentFactoryContext context)
	{
		if (context.Nodes.Length == 1 && context.Nodes[0] is HexViewerNode node)
		{
			return new HexViewerDocumentTabContent(node, _editorFactory, _bufferFactory);
		}

		return null;
	}

	public Guid? Serialize(DocumentTabContent content, ISettingsSection section)
	{
		return null;
	}

	public DocumentTabContent? Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context)
	{
		return null;
	}
}

internal class HexViewerDocumentTabContent : DocumentTabContent
{
	private readonly HexViewerNode _node;
	private readonly HexEditorFactoryService _editorFactory;
	private readonly HexBufferFactoryService _bufferFactory;

	public HexViewerDocumentTabContent(HexViewerNode node, HexEditorFactoryService editorFactory,
		HexBufferFactoryService bufferFactory)
	{
		_node = node;
		_editorFactory = editorFactory;
		_bufferFactory = bufferFactory;
	}

	public override string Title => _node.GetName();

	public override DocumentTabContent Clone()
		=> new HexViewerDocumentTabContent(_node, _editorFactory, _bufferFactory);

	public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator)
	{
		var buffer = _bufferFactory.Create(_node.GetHexData(), _node.GetName());

		var thing = _editorFactory.Create(buffer);
		return new HexViewerDocumentTabUiContext(thing);
	}
}

internal class HexViewerDocumentTabUiContext : DocumentTabUIContext
{
	public HexViewerDocumentTabUiContext(WpfHexView uiObject)
	{
		UIObject = uiObject.VisualElement;
	}

	public override object? UIObject { get; }
	public override IInputElement? FocusedElement => null;
	public override FrameworkElement? ZoomElement => null;
}
