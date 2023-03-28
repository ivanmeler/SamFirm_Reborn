using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SamFirm
{
  public class Form1 : Form
  {
    private bool SaveFileDialog = true;
    private Command.Firmware FW;
    public bool PauseDownload;
    private string destinationfile;
    private IContainer components;
    private ComboBox model_textbox;
    private Label model_lbl;
    private Button download_button;
    public RichTextBox log_textbox;
    private Label region_lbl;
    private ComboBox region_textbox;
    private Label pda_lbl;
    private TextBox pda_textbox;
    private Label csc_lbl;
    private TextBox csc_textbox;
    private Button update_button;
    private Label phone_lbl;
    private TextBox phone_textbox;
    private Label file_lbl;
    private TextBox file_textbox;
    private Label version_lbl;
    private TextBox version_textbox;
    private GroupBox groupBox1;
    private CheckBox binary_checkbox;
    private Label binary_lbl;
    private ProgressBar progressBar;
    private Button decrypt_button;
    private GroupBox groupBox2;
    private TextBox size_textbox;
    private Label size_lbl;
    private GroupBox groupBox3;
    private CheckBox checkbox_manual;
    private CheckBox checkbox_auto;
    private CheckBox checkbox_autodecrypt;
    private CheckBox checkbox_crc;
    private ToolTip tooltip_binary;
    public Label lbl_speed;
    private Label label1;
    private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        public Label lbl_transferred;
    private ToolTip tooltip_binary_box;

    public Form1()
    {
      this.InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      Logger.form = this;
      Web.form = this;
      Crypto.form = this;
      string[] models = Settings.ReadSetting<string[]>("Models");
      if (models?.Length > 0)
      {
        this.model_textbox.Items.Clear();
        this.model_textbox.Items.AddRange(models);
      }
      this.model_textbox.Text = Settings.ReadSetting<string>("Model");
      string[] regions = Settings.ReadSetting<string[]>("Regions");
      if (regions?.Length > 0)
      {
        this.region_textbox.Items.Clear();
        this.region_textbox.Items.AddRange(regions);
      }
      this.region_textbox.Text = Settings.ReadSetting<string>("Region");
      this.pda_textbox.Text = Settings.ReadSetting<string>("PDAVer");
      this.csc_textbox.Text = Settings.ReadSetting<string>("CSCVer");
      this.phone_textbox.Text = Settings.ReadSetting<string>("PHONEVer");
      if (Settings.ReadSetting<string>("AutoInfo").ToLower() == "true")
        this.checkbox_auto.Checked = true;
      else
        this.checkbox_manual.Checked = true;
      if (Settings.ReadSetting<string>("SaveFileDialog").ToLower() == "false")
        this.SaveFileDialog = false;
      if (Settings.ReadSetting<string>("BinaryNature").ToLower() == "true")
        this.binary_checkbox.Checked = true;
      if (Settings.ReadSetting<string>("CheckCRC").ToLower() == "false")
        this.checkbox_crc.Checked = false;
      if (Settings.ReadSetting<string>("AutoDecrypt").ToLower() == "false")
        this.checkbox_autodecrypt.Checked = false;
      this.tooltip_binary.SetToolTip((Control) this.binary_lbl, "Full firmware including PIT file");
      this.tooltip_binary_box.SetToolTip((Control) this.binary_checkbox, "Full firmware including PIT file");
      Logger.WriteLog("SamFirm v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, false);
      ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback) ((senderX, certificate, chain, sslPolicyErrors) => true);
    }

    private void Form1_Close(object sender, EventArgs e)
    {
      try
      {
        Settings.SetSetting("Model", this.model_textbox.Text.ToUpper());
        Settings.SetSetting("Region", this.region_textbox.Text.ToUpper());
        Settings.SetSetting("PDAVer", this.pda_textbox.Text);
        Settings.SetSetting("CSCVer", this.csc_textbox.Text);
        Settings.SetSetting("PHONEVer", this.phone_textbox.Text);
        Settings.SetSetting("AutoInfo", this.checkbox_auto.Checked.ToString());
        Settings.SetSetting("SaveFileDialog", this.SaveFileDialog.ToString());
        Settings.SetSetting("BinaryNature", this.binary_checkbox.Checked.ToString());
        Settings.SetSetting("CheckCRC", this.checkbox_crc.Checked.ToString());
        Settings.SetSetting("AutoDecrypt", this.checkbox_autodecrypt.Checked.ToString());
      }
      catch { }
      this.PauseDownload = true;
      Thread.Sleep(100);
      Imports.FreeModule();
      Logger.SaveLog();
    }

    private void download_button_Click(object sender, EventArgs e)
    {
      if (this.download_button.Text == "Pause")
      {
        Utility.TaskBarProgressState(true);
        this.PauseDownload = true;
        Utility.ReconnectDownload = false;
        this.download_button.Text = "Download";
      }
      else
      {
        if (e != null && e.GetType() == typeof (Form1.DownloadEventArgs) && ((Form1.DownloadEventArgs) e).isReconnect && (this.download_button.Text == "Pause" || !Utility.ReconnectDownload))
          return;
        if (this.PauseDownload)
          Logger.WriteLog("Download thread is still running. Please wait.", false);
        else if (string.IsNullOrEmpty(this.file_textbox.Text))
        {
          Logger.WriteLog("No file to download. Please check for update first.", false);
        }
        else
        {
          if (e.GetType() != typeof (Form1.DownloadEventArgs) || !((Form1.DownloadEventArgs) e).isReconnect)
          {
            if (this.SaveFileDialog)
            {
              string str = Path.GetExtension(Path.GetFileNameWithoutExtension(this.FW.Filename)) + Path.GetExtension(this.FW.Filename);
              this.saveFileDialog1.SupportMultiDottedExtensions = true;
              this.saveFileDialog1.OverwritePrompt = false;
              this.saveFileDialog1.FileName = this.FW.Filename.Replace(str, "");
              this.saveFileDialog1.Filter = "Firmware|*" + str;
              if (this.saveFileDialog1.ShowDialog() != DialogResult.OK)
              {
                Logger.WriteLog("Aborted.", false);
                return;
              }
              if (!this.saveFileDialog1.FileName.EndsWith(str))
                this.saveFileDialog1.FileName += str;
              else
                this.saveFileDialog1.FileName = this.saveFileDialog1.FileName.Replace(str + str, str);
              Logger.WriteLog("Filename: " + this.saveFileDialog1.FileName, false);
              this.destinationfile = this.saveFileDialog1.FileName;
              if (System.IO.File.Exists(this.destinationfile))
              {
                switch (new customMessageBox("The destination file already exists.\r\nWould you like to append it (resume download)?", "Append", DialogResult.Yes, "Overwrite", DialogResult.No, "Cancel", DialogResult.Cancel, (Image) SystemIcons.Warning.ToBitmap()).ShowDialog())
                {
                  case DialogResult.Cancel:
                    Logger.WriteLog("Aborted.", false);
                    return;
                  case DialogResult.No:
                    System.IO.File.Delete(this.destinationfile);
                    break;
                }
              }
            }
            else
              this.destinationfile = this.FW.Filename;
          }
          Utility.TaskBarProgressState(false);
          BackgroundWorker backgroundWorker = new BackgroundWorker();
          backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
          {
            try
            {
              this.ControlsEnabled(false);
              Utility.ReconnectDownload = false;
              this.download_button.Invoke((Delegate)((Action)(() =>
              {
                this.download_button.Enabled = true;
                this.download_button.Text = "Pause";
              })));
              if (this.FW.Filename == this.destinationfile)
                Logger.WriteLog("Trying to download " + this.FW.Filename, false);
              else
                Logger.WriteLog("Trying to download " + this.FW.Filename + " to " + this.destinationfile, false);
              Command.Download(this.FW.Path, this.FW.Filename, this.FW.Version, this.FW.Region, this.FW.Model_Type, this.destinationfile, this.FW.Size, true);
              if (this.PauseDownload)
              {
                Logger.WriteLog("Download paused", false);
                this.PauseDownload = false;
                if (Utility.ReconnectDownload)
                {
                  Logger.WriteLog("Reconnecting...", false);
                  Utility.Reconnect(new Action<object, EventArgs>(this.download_button_Click));
                }
              }
              else
              {
                Logger.WriteLog("Download finished", false);
                if (this.checkbox_crc.Checked)
                {
                  if (this.FW.CRC == null)
                  {
                    Logger.WriteLog("Unable to check CRC. Value not set by Samsung", false);
                  }
                  else
                  {
                    Logger.WriteLog("\nChecking CRC32...", false);
                    if (!Utility.CRCCheck(this.destinationfile, this.FW.CRC))
                    {
                      Logger.WriteLog("Error: CRC does not match. Please redownload the file.", false);
                      System.IO.File.Delete(this.destinationfile);
                      goto label_15;
                    }
                    else
                      Logger.WriteLog("Success: CRC match!", false);
                  }
                }
                this.decrypt_button.Invoke((Delegate)((Action)(() => this.decrypt_button.Enabled = true)));
                if (this.checkbox_autodecrypt.Checked)
                  this.decrypt_button_Click(o, (EventArgs) null);
              }
label_15:
              if (!Utility.ReconnectDownload)
                this.ControlsEnabled(true);
              this.download_button.Invoke((Delegate)((Action)(() => this.download_button.Text = "Download")));
            }
            catch (Exception ex)
            {
              Logger.WriteLog(ex.Message, false);
              Logger.WriteLog(ex.ToString(), false);
            }
          });
          backgroundWorker.RunWorkerAsync();
        }
      }
    }

    private void update_button_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(this.model_textbox.Text))
        Logger.WriteLog("Error: Please specify a model", false);
      else if (string.IsNullOrEmpty(this.region_textbox.Text))
        Logger.WriteLog("Error: Please specify a region", false);
      else if (this.checkbox_manual.Checked && (string.IsNullOrEmpty(this.pda_textbox.Text) || string.IsNullOrEmpty(this.csc_textbox.Text) || string.IsNullOrEmpty(this.phone_textbox.Text)))
      {
        Logger.WriteLog("Error: Please specify PDA, CSC and Phone version or use Auto Method", false);
      }
      else
      {
        string model = this.model_textbox.Text.ToUpper();
        string region = this.region_textbox.Text.ToUpper();
        BackgroundWorker backgroundWorker = new BackgroundWorker();
        backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
        {
          try
          {
            this.SetProgressBar(0, 0);
            this.ControlsEnabled(false);
            Utility.ReconnectDownload = false;
            this.FW = !this.checkbox_auto.Checked ? Command.UpdateCheck(model, region, this.pda_textbox.Text, this.csc_textbox.Text, this.phone_textbox.Text, this.pda_textbox.Text, this.binary_checkbox.Checked, false) : Command.UpdateCheckAuto(model, region, this.binary_checkbox.Checked);
            if (!string.IsNullOrEmpty(this.FW.Filename))
            {
              this.file_textbox.Invoke((Delegate)((Action)(() => this.file_textbox.Text = this.FW.Filename)));
              this.version_textbox.Invoke((Delegate)((Action)(() => this.version_textbox.Text = this.FW.Version)));
              this.size_textbox.Invoke((Delegate)((Action)(() => this.size_textbox.Text = (long.Parse(this.FW.Size) / 1024L / 1024L).ToString() + " MB")));
              this.model_textbox.Invoke((Action)(() =>
              {
                var items = model_textbox.Items.OfType<string>().ToList();
                items.Add(model);
                Settings.SetSetting("Models", items.Distinct().OrderBy(s => s));
                items = region_textbox.Items.OfType<string>().ToList();
                items.Add(region);
                Settings.SetSetting("Regions", items.Distinct().OrderBy(s => s));
              }));
            }
            else
            {
              this.file_textbox.Invoke((Delegate)((Action)(() => this.file_textbox.Text = string.Empty)));
              this.version_textbox.Invoke((Delegate)((Action)(() => this.version_textbox.Text = string.Empty)));
              this.size_textbox.Invoke((Delegate)((Action)(() => this.size_textbox.Text = string.Empty)));
            }
            this.ControlsEnabled(true);
          }
          catch (Exception ex)
          {
            Logger.WriteLog(ex.Message, false);
            Logger.WriteLog(ex.ToString(), false);
          }
        });
        backgroundWorker.RunWorkerAsync();
      }
    }

    public void SetProgressBar(int Progress, long bytesTransferred)
    {
      if (Progress > 100)
        Progress = 100;
      this.progressBar.Invoke((Delegate)((Action)(() =>
      {
        this.progressBar.Value = Progress;
        if (bytesTransferred > 0)
        {
          this.lbl_transferred.Text = $"{bytesTransferred / 1024.0 / 1024.0:0.00} MB";
        }
        else 
        {
          this.lbl_transferred.Text = "";
        }
        try
        {
          TaskbarManager.Instance.SetProgressValue(Progress, 100);
        }
        catch (Exception ex)
        {
        }
      })));
    }

    private void ControlsEnabled(bool Enabled)
    {
      this.update_button.Invoke((Delegate)((Action)(() => this.update_button.Enabled = Enabled)));
      this.download_button.Invoke((Delegate)((Action)(() => this.download_button.Enabled = Enabled)));
      this.binary_checkbox.Invoke((Delegate)((Action)(() => this.binary_checkbox.Enabled = Enabled)));
      this.model_textbox.Invoke((Delegate)((Action)(() => this.model_textbox.Enabled = Enabled)));
      this.region_textbox.Invoke((Delegate)((Action)(() => this.region_textbox.Enabled = Enabled)));
      this.checkbox_auto.Invoke((Delegate)((Action)(() => this.checkbox_auto.Enabled = Enabled)));
      this.checkbox_manual.Invoke((Delegate)((Action)(() => this.checkbox_manual.Enabled = Enabled)));
      this.checkbox_manual.Invoke((Delegate)((Action)(() =>
      {
        if (!this.checkbox_manual.Checked)
          return;
        this.pda_textbox.Enabled = Enabled;
        this.csc_textbox.Enabled = Enabled;
        this.phone_textbox.Enabled = Enabled;
      })));
      this.checkbox_autodecrypt.Invoke((Delegate)((Action)(() => this.checkbox_autodecrypt.Enabled = Enabled)));
      this.checkbox_crc.Invoke((Delegate)((Action)(() => this.checkbox_crc.Enabled = Enabled)));
    }

    private void decrypt_button_Click(object sender, EventArgs e)
    {
      if (!System.IO.File.Exists(this.destinationfile))
      {
        Logger.WriteLog("Error: File " + this.destinationfile + " does not exist", false);
      }
      else
      {
        string pda = this.pda_textbox.Text;
        string csc = this.csc_textbox.Text;
        string phone = this.phone_textbox.Text;

        BackgroundWorker backgroundWorker = new BackgroundWorker();
        backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
        {
          Thread.Sleep(100);
          Logger.WriteLog("\nDecrypting and unzipping firmware...", false);
          this.ControlsEnabled(false);
          this.decrypt_button.Invoke((Delegate)((Action)(() => this.decrypt_button.Enabled = false)));
          if (this.destinationfile.EndsWith(".enc2"))
            Crypto.SetDecryptKey(this.FW.Region, this.FW.Model, this.FW.Version);
          else if (this.destinationfile.EndsWith(".enc4"))
          {
            if (this.FW.BinaryNature == 1)
              Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueFactory);
            else
              Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueHome);
          }
          string outputDirectory = Path.Combine(Path.GetDirectoryName(this.destinationfile), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(this.destinationfile)));
          if (Crypto.DecryptAndUnzip(this.destinationfile, outputDirectory, true) == 0)
          {
            CmdLine.SaveMeta(FW, Path.Combine(outputDirectory, "FirmwareInfo.txt"));
//            File.WriteAllText(Path.Combine(outputDirectory, "FirmwareInfo,txt"), $@"
//Model: {FW.Model}
//Type: {FW.Model_Type}
//Date: {FW.LastModified}
//DisplayName: {FW.DisplayName}
//OS: {FW.OS}
//Region: {FW.Region}
//Version: {FW.Version}
//PDA: {pda}
//CSC: {csc}
//Phone: {phone}
//");
            System.IO.File.Delete(this.destinationfile);
          }
          Logger.WriteLog("Decryption finished", false);
          this.ControlsEnabled(true);
        });
        backgroundWorker.RunWorkerAsync();
      }
    }

    private void checkbox_manual_CheckedChanged(object sender, EventArgs e)
    {
      if (!this.checkbox_auto.Checked && !this.checkbox_manual.Checked)
      {
        this.checkbox_manual.Checked = true;
      }
      else
      {
        this.checkbox_auto.Checked = !this.checkbox_manual.Checked;
        this.pda_textbox.Enabled = this.checkbox_manual.Checked;
        this.csc_textbox.Enabled = this.checkbox_manual.Checked;
        this.phone_textbox.Enabled = this.checkbox_manual.Checked;
      }
    }

    private void checkbox_auto_CheckedChanged(object sender, EventArgs e)
    {
      if (!this.checkbox_manual.Checked && !this.checkbox_auto.Checked)
      {
        this.checkbox_auto.Checked = true;
      }
      else
      {
        this.checkbox_manual.Checked = !this.checkbox_auto.Checked;
        this.pda_textbox.Enabled = !this.checkbox_auto.Checked;
        this.csc_textbox.Enabled = !this.checkbox_auto.Checked;
        this.phone_textbox.Enabled = !this.checkbox_auto.Checked;
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
      this.model_textbox = new System.Windows.Forms.ComboBox();
      this.model_lbl = new System.Windows.Forms.Label();
      this.download_button = new System.Windows.Forms.Button();
      this.log_textbox = new System.Windows.Forms.RichTextBox();
      this.region_lbl = new System.Windows.Forms.Label();
      this.region_textbox = new System.Windows.Forms.ComboBox();
      this.pda_lbl = new System.Windows.Forms.Label();
      this.pda_textbox = new System.Windows.Forms.TextBox();
      this.csc_lbl = new System.Windows.Forms.Label();
      this.csc_textbox = new System.Windows.Forms.TextBox();
      this.update_button = new System.Windows.Forms.Button();
      this.phone_lbl = new System.Windows.Forms.Label();
      this.phone_textbox = new System.Windows.Forms.TextBox();
      this.file_lbl = new System.Windows.Forms.Label();
      this.file_textbox = new System.Windows.Forms.TextBox();
      this.version_lbl = new System.Windows.Forms.Label();
      this.version_textbox = new System.Windows.Forms.TextBox();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.groupBox3 = new System.Windows.Forms.GroupBox();
      this.checkbox_manual = new System.Windows.Forms.CheckBox();
      this.checkbox_auto = new System.Windows.Forms.CheckBox();
      this.binary_checkbox = new System.Windows.Forms.CheckBox();
      this.binary_lbl = new System.Windows.Forms.Label();
      this.progressBar = new System.Windows.Forms.ProgressBar();
      this.decrypt_button = new System.Windows.Forms.Button();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.lbl_transferred = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.lbl_speed = new System.Windows.Forms.Label();
      this.checkbox_autodecrypt = new System.Windows.Forms.CheckBox();
      this.checkbox_crc = new System.Windows.Forms.CheckBox();
      this.size_textbox = new System.Windows.Forms.TextBox();
      this.size_lbl = new System.Windows.Forms.Label();
      this.tooltip_binary = new System.Windows.Forms.ToolTip(this.components);
      this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
      this.tooltip_binary_box = new System.Windows.Forms.ToolTip(this.components);
      this.groupBox1.SuspendLayout();
      this.groupBox3.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.SuspendLayout();
      // 
      // model_textbox
      // 
      //this.model_textbox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
      this.model_textbox.Items.AddRange(new object[] {
            "SM-G960F",
            "SM-G973F",
            "SM-S901B",
            "SM-G991B",
            "SM-G998B",
            "SM-S911B",
            "SM-T395",
            "SM-T545",
            "SM-T575"});
      this.model_textbox.Location = new System.Drawing.Point(85, 22);
      this.model_textbox.Name = "model_textbox";
      this.model_textbox.Size = new System.Drawing.Size(149, 20);
      this.model_textbox.TabIndex = 0;
      // 
      // model_lbl
      // 
      this.model_lbl.AutoSize = true;
      this.model_lbl.Location = new System.Drawing.Point(8, 25);
      this.model_lbl.Name = "model_lbl";
      this.model_lbl.Size = new System.Drawing.Size(36, 13);
      this.model_lbl.TabIndex = 1;
      this.model_lbl.Text = "Model";
      // 
      // download_button
      // 
      this.download_button.Location = new System.Drawing.Point(74, 117);
      this.download_button.Margin = new System.Windows.Forms.Padding(0);
      this.download_button.Name = "download_button";
      this.download_button.Size = new System.Drawing.Size(94, 23);
      this.download_button.TabIndex = 13;
      this.download_button.Text = "Download";
      this.download_button.UseVisualStyleBackColor = true;
      this.download_button.Click += new System.EventHandler(this.download_button_Click);
      // 
      // log_textbox
      // 
      this.log_textbox.Location = new System.Drawing.Point(12, 233);
      this.log_textbox.Name = "log_textbox";
      this.log_textbox.ReadOnly = true;
      this.log_textbox.Size = new System.Drawing.Size(639, 138);
      this.log_textbox.TabIndex = 3;
      this.log_textbox.TabStop = false;
      this.log_textbox.Text = "";
      // 
      // region_lbl
      // 
      this.region_lbl.AutoSize = true;
      this.region_lbl.Location = new System.Drawing.Point(8, 51);
      this.region_lbl.Name = "region_lbl";
      this.region_lbl.Size = new System.Drawing.Size(41, 13);
      this.region_lbl.TabIndex = 5;
      this.region_lbl.Text = "Region";
      // 
      // region_textbox
      // 
      this.region_textbox.Items.AddRange(new object[] {
            "NEE",
            "EUX"});
      this.region_textbox.Location = new System.Drawing.Point(85, 48);
      this.region_textbox.Name = "region_textbox";
      this.region_textbox.Size = new System.Drawing.Size(149, 21);
      this.region_textbox.TabIndex = 1;
      // 
      // pda_lbl
      // 
      this.pda_lbl.AutoSize = true;
      this.pda_lbl.Location = new System.Drawing.Point(10, 15);
      this.pda_lbl.Name = "pda_lbl";
      this.pda_lbl.Size = new System.Drawing.Size(29, 13);
      this.pda_lbl.TabIndex = 7;
      this.pda_lbl.Text = "PDA";
      // 
      // pda_textbox
      // 
      this.pda_textbox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
      this.pda_textbox.Location = new System.Drawing.Point(79, 12);
      this.pda_textbox.Name = "pda_textbox";
      this.pda_textbox.Size = new System.Drawing.Size(149, 20);
      this.pda_textbox.TabIndex = 4;
      // 
      // csc_lbl
      // 
      this.csc_lbl.AutoSize = true;
      this.csc_lbl.Location = new System.Drawing.Point(10, 41);
      this.csc_lbl.Name = "csc_lbl";
      this.csc_lbl.Size = new System.Drawing.Size(28, 13);
      this.csc_lbl.TabIndex = 9;
      this.csc_lbl.Text = "CSC";
      // 
      // csc_textbox
      // 
      this.csc_textbox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
      this.csc_textbox.Location = new System.Drawing.Point(79, 38);
      this.csc_textbox.Name = "csc_textbox";
      this.csc_textbox.Size = new System.Drawing.Size(149, 20);
      this.csc_textbox.TabIndex = 5;
      // 
      // update_button
      // 
      this.update_button.Location = new System.Drawing.Point(141, 186);
      this.update_button.Name = "update_button";
      this.update_button.Size = new System.Drawing.Size(93, 23);
      this.update_button.TabIndex = 10;
      this.update_button.Text = "Check Update";
      this.update_button.UseVisualStyleBackColor = true;
      this.update_button.Click += new System.EventHandler(this.update_button_Click);
      // 
      // phone_lbl
      // 
      this.phone_lbl.AutoSize = true;
      this.phone_lbl.Location = new System.Drawing.Point(10, 67);
      this.phone_lbl.Name = "phone_lbl";
      this.phone_lbl.Size = new System.Drawing.Size(38, 13);
      this.phone_lbl.TabIndex = 12;
      this.phone_lbl.Text = "Phone";
      // 
      // phone_textbox
      // 
      this.phone_textbox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
      this.phone_textbox.Location = new System.Drawing.Point(79, 64);
      this.phone_textbox.Name = "phone_textbox";
      this.phone_textbox.Size = new System.Drawing.Size(149, 20);
      this.phone_textbox.TabIndex = 6;
      // 
      // file_lbl
      // 
      this.file_lbl.AutoSize = true;
      this.file_lbl.Location = new System.Drawing.Point(6, 25);
      this.file_lbl.Name = "file_lbl";
      this.file_lbl.Size = new System.Drawing.Size(23, 13);
      this.file_lbl.TabIndex = 13;
      this.file_lbl.Text = "File";
      // 
      // file_textbox
      // 
      this.file_textbox.Location = new System.Drawing.Point(75, 18);
      this.file_textbox.Name = "file_textbox";
      this.file_textbox.ReadOnly = true;
      this.file_textbox.Size = new System.Drawing.Size(290, 20);
      this.file_textbox.TabIndex = 20;
      this.file_textbox.TabStop = false;
      // 
      // version_lbl
      // 
      this.version_lbl.AutoSize = true;
      this.version_lbl.Location = new System.Drawing.Point(6, 51);
      this.version_lbl.Name = "version_lbl";
      this.version_lbl.Size = new System.Drawing.Size(42, 13);
      this.version_lbl.TabIndex = 15;
      this.version_lbl.Text = "Version";
      // 
      // version_textbox
      // 
      this.version_textbox.Location = new System.Drawing.Point(75, 44);
      this.version_textbox.Name = "version_textbox";
      this.version_textbox.ReadOnly = true;
      this.version_textbox.Size = new System.Drawing.Size(290, 20);
      this.version_textbox.TabIndex = 30;
      this.version_textbox.TabStop = false;
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.groupBox3);
      this.groupBox1.Controls.Add(this.checkbox_manual);
      this.groupBox1.Controls.Add(this.checkbox_auto);
      this.groupBox1.Controls.Add(this.binary_checkbox);
      this.groupBox1.Controls.Add(this.binary_lbl);
      this.groupBox1.Controls.Add(this.model_textbox);
      this.groupBox1.Controls.Add(this.model_lbl);
      this.groupBox1.Controls.Add(this.update_button);
      this.groupBox1.Controls.Add(this.region_textbox);
      this.groupBox1.Controls.Add(this.region_lbl);
      this.groupBox1.Location = new System.Drawing.Point(12, 12);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(262, 215);
      this.groupBox1.TabIndex = 17;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Firmware Info";
      // 
      // groupBox3
      // 
      this.groupBox3.Controls.Add(this.phone_textbox);
      this.groupBox3.Controls.Add(this.csc_lbl);
      this.groupBox3.Controls.Add(this.csc_textbox);
      this.groupBox3.Controls.Add(this.pda_lbl);
      this.groupBox3.Controls.Add(this.pda_textbox);
      this.groupBox3.Controls.Add(this.phone_lbl);
      this.groupBox3.Location = new System.Drawing.Point(6, 93);
      this.groupBox3.Name = "groupBox3";
      this.groupBox3.Size = new System.Drawing.Size(250, 89);
      this.groupBox3.TabIndex = 17;
      this.groupBox3.TabStop = false;
      // 
      // checkbox_manual
      // 
      this.checkbox_manual.AutoSize = true;
      this.checkbox_manual.Location = new System.Drawing.Point(129, 75);
      this.checkbox_manual.Name = "checkbox_manual";
      this.checkbox_manual.Size = new System.Drawing.Size(61, 17);
      this.checkbox_manual.TabIndex = 3;
      this.checkbox_manual.Text = "Manual";
      this.checkbox_manual.UseVisualStyleBackColor = true;
      this.checkbox_manual.CheckedChanged += new System.EventHandler(this.checkbox_manual_CheckedChanged);
      // 
      // checkbox_auto
      // 
      this.checkbox_auto.AutoSize = true;
      this.checkbox_auto.Location = new System.Drawing.Point(11, 75);
      this.checkbox_auto.Name = "checkbox_auto";
      this.checkbox_auto.Size = new System.Drawing.Size(48, 17);
      this.checkbox_auto.TabIndex = 2;
      this.checkbox_auto.Text = "Auto";
      this.checkbox_auto.UseVisualStyleBackColor = true;
      this.checkbox_auto.CheckedChanged += new System.EventHandler(this.checkbox_auto_CheckedChanged);
      // 
      // binary_checkbox
      // 
      this.binary_checkbox.AutoSize = true;
      this.binary_checkbox.Location = new System.Drawing.Point(85, 191);
      this.binary_checkbox.Name = "binary_checkbox";
      this.binary_checkbox.Size = new System.Drawing.Size(15, 14);
      this.binary_checkbox.TabIndex = 7;
      this.binary_checkbox.UseVisualStyleBackColor = true;
      // 
      // binary_lbl
      // 
      this.binary_lbl.AutoSize = true;
      this.binary_lbl.Location = new System.Drawing.Point(8, 190);
      this.binary_lbl.Name = "binary_lbl";
      this.binary_lbl.Size = new System.Drawing.Size(71, 13);
      this.binary_lbl.TabIndex = 13;
      this.binary_lbl.Text = "Binary Nature";
      // 
      // progressBar
      // 
      this.progressBar.Location = new System.Drawing.Point(75, 146);
      this.progressBar.Name = "progressBar";
      this.progressBar.Size = new System.Drawing.Size(290, 23);
      this.progressBar.TabIndex = 18;
      // 
      // decrypt_button
      // 
      this.decrypt_button.Enabled = false;
      this.decrypt_button.Location = new System.Drawing.Point(188, 117);
      this.decrypt_button.Name = "decrypt_button";
      this.decrypt_button.Size = new System.Drawing.Size(127, 23);
      this.decrypt_button.TabIndex = 14;
      this.decrypt_button.Text = "Decrypt";
      this.decrypt_button.UseVisualStyleBackColor = true;
      this.decrypt_button.Click += new System.EventHandler(this.decrypt_button_Click);
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.lbl_transferred);
      this.groupBox2.Controls.Add(this.label1);
      this.groupBox2.Controls.Add(this.lbl_speed);
      this.groupBox2.Controls.Add(this.checkbox_autodecrypt);
      this.groupBox2.Controls.Add(this.checkbox_crc);
      this.groupBox2.Controls.Add(this.size_textbox);
      this.groupBox2.Controls.Add(this.size_lbl);
      this.groupBox2.Controls.Add(this.progressBar);
      this.groupBox2.Controls.Add(this.decrypt_button);
      this.groupBox2.Controls.Add(this.download_button);
      this.groupBox2.Controls.Add(this.file_lbl);
      this.groupBox2.Controls.Add(this.file_textbox);
      this.groupBox2.Controls.Add(this.version_textbox);
      this.groupBox2.Controls.Add(this.version_lbl);
      this.groupBox2.Location = new System.Drawing.Point(280, 12);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(371, 215);
      this.groupBox2.TabIndex = 20;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Download";
      // 
      // lbl_transferred
      // 
      this.lbl_transferred.AutoSize = true;
      this.lbl_transferred.Location = new System.Drawing.Point(185, 176);
      this.lbl_transferred.Name = "lbl_transferred";
      this.lbl_transferred.Size = new System.Drawing.Size(32, 13);
      this.lbl_transferred.TabIndex = 41;
      this.lbl_transferred.Text = "0 MB";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(6, 176);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(38, 13);
      this.label1.TabIndex = 25;
      this.label1.Text = "Speed";
      // 
      // lbl_speed
      // 
      this.lbl_speed.AutoSize = true;
      this.lbl_speed.Location = new System.Drawing.Point(72, 176);
      this.lbl_speed.Name = "lbl_speed";
      this.lbl_speed.Size = new System.Drawing.Size(40, 13);
      this.lbl_speed.TabIndex = 24;
      this.lbl_speed.Text = "0 KB/s";
      // 
      // checkbox_autodecrypt
      // 
      this.checkbox_autodecrypt.AutoSize = true;
      this.checkbox_autodecrypt.Checked = true;
      this.checkbox_autodecrypt.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkbox_autodecrypt.Location = new System.Drawing.Point(189, 96);
      this.checkbox_autodecrypt.Name = "checkbox_autodecrypt";
      this.checkbox_autodecrypt.Size = new System.Drawing.Size(127, 17);
      this.checkbox_autodecrypt.TabIndex = 12;
      this.checkbox_autodecrypt.Text = "Decrypt automatically";
      this.checkbox_autodecrypt.UseVisualStyleBackColor = true;
      // 
      // checkbox_crc
      // 
      this.checkbox_crc.AutoSize = true;
      this.checkbox_crc.Checked = true;
      this.checkbox_crc.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkbox_crc.Location = new System.Drawing.Point(75, 96);
      this.checkbox_crc.Name = "checkbox_crc";
      this.checkbox_crc.Size = new System.Drawing.Size(94, 17);
      this.checkbox_crc.TabIndex = 11;
      this.checkbox_crc.Text = "Check CRC32";
      this.checkbox_crc.UseVisualStyleBackColor = true;
      // 
      // size_textbox
      // 
      this.size_textbox.Location = new System.Drawing.Point(75, 70);
      this.size_textbox.Name = "size_textbox";
      this.size_textbox.ReadOnly = true;
      this.size_textbox.Size = new System.Drawing.Size(290, 20);
      this.size_textbox.TabIndex = 40;
      this.size_textbox.TabStop = false;
      // 
      // size_lbl
      // 
      this.size_lbl.AutoSize = true;
      this.size_lbl.Location = new System.Drawing.Point(6, 75);
      this.size_lbl.Name = "size_lbl";
      this.size_lbl.Size = new System.Drawing.Size(27, 13);
      this.size_lbl.TabIndex = 20;
      this.size_lbl.Text = "Size";
      // 
      // saveFileDialog1
      // 
      this.saveFileDialog1.SupportMultiDottedExtensions = true;
      // 
      // Form1
      // 
      this.AcceptButton = this.update_button;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(668, 383);
      this.Controls.Add(this.groupBox2);
      this.Controls.Add(this.groupBox1);
      this.Controls.Add(this.log_textbox);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "Form1";
      this.Text = "SamFirm (BornAgain Edition)";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Close);
      this.Load += new System.EventHandler(this.Form1_Load);
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.groupBox3.ResumeLayout(false);
      this.groupBox3.PerformLayout();
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.ResumeLayout(false);

    }

    public class DownloadEventArgs : EventArgs
    {
      public bool isReconnect;
    }
  }
}
