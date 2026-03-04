using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NSMBe5
{
    internal sealed class DockableEditorWindow : DockContent
    {
        private readonly string persistKey;
        private readonly DockPanel dockPanel;
        private readonly DockState defaultDockState;
        private readonly ContextMenuStrip windowMenu;

        public DockableEditorWindow(string persistKey, string title, Control content, DockPanel dockPanel, DockState defaultDockState)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (dockPanel == null)
                throw new ArgumentNullException(nameof(dockPanel));

            this.persistKey = persistKey;
            this.dockPanel = dockPanel;
            this.defaultDockState = defaultDockState;

            Text = title;
            TabText = title;
            HideOnClose = true;
            CloseButton = false;
            CloseButtonVisible = false;
            DockHandler.CloseButton = false;
            DockHandler.CloseButtonVisible = false;
            ShowHint = defaultDockState;

            if (content.Parent != null)
                content.Parent.Controls.Remove(content);

            content.Dock = DockStyle.Fill;
            Controls.Add(content);

            windowMenu = new ContextMenuStrip();
            var undockItem = new ToolStripMenuItem("Desacoplar");
            undockItem.Click += (sender, e) => Show(this.dockPanel, DockState.Float);
            var resetDockItem = new ToolStripMenuItem("Acoplar");
            resetDockItem.Click += (sender, e) => RestoreDefaultDock();
            windowMenu.Items.Add(undockItem);
            windowMenu.Items.Add(resetDockItem);

            ContextMenuStrip = windowMenu;
        }

        protected override string GetPersistString()
        {
            return persistKey;
        }

        public void RestoreDefaultDock()
        {
            Show(dockPanel, defaultDockState);
        }
    }
}
