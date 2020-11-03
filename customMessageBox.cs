// Decompiled with JetBrains decompiler
// Type: SamFirm.customMessageBox
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SamFirm
{
  public class customMessageBox : Form
  {
    private IContainer components;
    private Button button1;
    private Button button2;
    private Button button3;
    private Label lbltext;
    private PictureBox pictureBox1;

    public customMessageBox()
    {
    }

    public customMessageBox(
      string message,
      string button1txt,
      DialogResult result1,
      string button2txt,
      DialogResult result2,
      string button3txt,
      DialogResult result3,
      Image img)
    {
      this.InitializeComponent();
      this.lbltext.Text = message;
      this.button1.Text = button1txt;
      this.button1.DialogResult = result1;
      if (result1 == DialogResult.Cancel)
        this.CancelButton = (IButtonControl) this.button1;
      this.button2.Text = button2txt;
      this.button2.DialogResult = result2;
      if (result2 == DialogResult.Cancel)
        this.CancelButton = (IButtonControl) this.button2;
      this.button3.Text = button3txt;
      this.button3.DialogResult = result3;
      if (result3 == DialogResult.Cancel)
        this.CancelButton = (IButtonControl) this.button3;
      this.pictureBox1.Image = img;
      this.Font = SystemFonts.DefaultFont;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof (customMessageBox));
      this.button1 = new Button();
      this.button2 = new Button();
      this.button3 = new Button();
      this.lbltext = new Label();
      this.pictureBox1 = new PictureBox();
      ((ISupportInitialize) this.pictureBox1).BeginInit();
      this.SuspendLayout();
      this.button1.Location = new Point(100, 60);
      this.button1.Name = "button1";
      this.button1.Size = new Size(75, 23);
      this.button1.TabIndex = 0;
      this.button1.Text = "button1";
      this.button1.UseVisualStyleBackColor = true;
      this.button2.Location = new Point(193, 60);
      this.button2.Name = "button2";
      this.button2.Size = new Size(75, 23);
      this.button2.TabIndex = 1;
      this.button2.Text = "button2";
      this.button2.UseVisualStyleBackColor = true;
      this.button3.Location = new Point(284, 60);
      this.button3.Name = "button3";
      this.button3.Size = new Size(75, 23);
      this.button3.TabIndex = 2;
      this.button3.Text = "button3";
      this.button3.UseVisualStyleBackColor = true;
      this.lbltext.AutoSize = true;
      this.lbltext.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point, (byte) 0);
      this.lbltext.Location = new Point(97, 12);
      this.lbltext.Name = "lbltext";
      this.lbltext.Size = new Size(26, 15);
      this.lbltext.TabIndex = 3;
      this.lbltext.Text = "text";
      this.pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      this.pictureBox1.Location = new Point(24, 12);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new Size(67, 58);
      this.pictureBox1.TabIndex = 4;
      this.pictureBox1.TabStop = false;
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.ClientSize = new Size(371, 94);
      this.Controls.Add((Control) this.pictureBox1);
      this.Controls.Add((Control) this.lbltext);
      this.Controls.Add((Control) this.button3);
      this.Controls.Add((Control) this.button2);
      this.Controls.Add((Control) this.button1);
      this.Icon = (Icon) componentResourceManager.GetObject("$this.Icon");
      this.MaximumSize = new Size(387, 133);
      this.MinimumSize = new Size(387, 133);
      this.Name = nameof (customMessageBox);
      this.Text = "SamFirm";
      ((ISupportInitialize) this.pictureBox1).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();
    }
  }
}
