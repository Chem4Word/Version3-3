using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Chem4Word.Core.UI.Controls
{
    public partial class EditorHostStatusPanel : UserControl
    {
        public event EventHandler OnClickOkEventHandler;

        public event EventHandler OnClickCancelEventHandler;

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label1Text
        {
            get => Label1.Text;
            set => Label1.Text = value;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label1ToolTip
        {
            get => ToolTip.GetToolTip(Label1);
            set => ToolTip.SetToolTip(Label1, value);
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public Color Label1Colour
        {
            get => Label1.ForeColor;
            set => Label1.ForeColor = value;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public bool Label1Bold
        {
            get => Label1.Font.Bold;
            set
            {
                if (value)
                {
                    Label1.Font = new Font(Label1.Font, FontStyle.Bold);
                }
                else
                {
                    Label1.Font = new Font(Label1.Font, FontStyle.Regular);
                }
            }
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label2Text
        {
            get => Label2.Text;
            set => Label2.Text = value;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label2ToolTip
        {
            get => ToolTip.GetToolTip(Label2);
            set => ToolTip.SetToolTip(Label2, value);
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public bool Label2Visible
        {
            get => Label2.Visible;
            set
            {
                LayoutPanel.ColumnStyles[1].SizeType = SizeType.Absolute;
                if (value)
                {
                    LayoutPanel.ColumnStyles[1].Width = 144;
                }
                else
                {
                    LayoutPanel.ColumnStyles[1].Width = 0;
                }

                Label2.Visible = value;
            }
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label3Text
        {
            get => Label3.Text;
            set => Label3.Text = value;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public string Label3ToolTip
        {
            get => ToolTip.GetToolTip(Label3);
            set => ToolTip.SetToolTip(Label3, value);
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Chem4Word")]
        public bool Label3Visible
        {
            get => Label3.Visible;
            set
            {
                LayoutPanel.ColumnStyles[2].SizeType = SizeType.Absolute;
                if (value)
                {
                    LayoutPanel.ColumnStyles[2].Width = 144;
                }
                else
                {
                    LayoutPanel.ColumnStyles[2].Width = 0;
                }

                Label3.Visible = value;
            }
        }

        public EditorHostStatusPanel()
        {
            InitializeComponent();
        }

        private void OnClick_Ok(object sender, EventArgs e)
        {
            OnClickOkEventHandler?.Invoke(sender, e);
        }

        private void OnClick_Cancel(object sender, EventArgs e)
        {
            OnClickCancelEventHandler?.Invoke(sender, e);
        }
    }
}