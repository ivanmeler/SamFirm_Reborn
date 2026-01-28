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
        private bool saveFileDialogEnabled = true;
        private Command.Firmware firmware;
        public bool PauseDownload;
        private string destinationFile;
        private IContainer components;
        private ComboBox comboBoxModel;
        private Label labelModel;
        private Button buttonDownload;
        public RichTextBox richTextBoxLog;
        private Label labelRegion;
        private ComboBox comboBoxRegion;
        private Label labelPda;
        private TextBox textBoxPda;
        private Label labelCsc;
        private TextBox textBoxCsc;
        private Button buttonUpdate;
        private Label labelPhone;
        private TextBox textBoxPhone;
        private Label labelFile;
        private TextBox textBoxFile;
        private Label labelVersion;
        private TextBox textBoxVersion;
        private GroupBox groupBoxInfo;
        private CheckBox checkBoxBinary;
        private Label labelBinary;
        private ProgressBar progressBar;
        private Button buttonDecrypt;
        private GroupBox groupBoxDownload;
        private TextBox textBoxSize;
        private Label labelSize;
        private GroupBox groupBoxManual;
        private CheckBox checkBoxManual;
        private CheckBox checkBoxAuto;
        private CheckBox checkBoxAutoDecrypt;
        private CheckBox checkBoxCrc;
        private ToolTip tooltipBinary;
        public Label labelSpeed;
        private Label labelSpeedTitle;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        public Label labelTransferred;
        private Label labelImei;
        private TextBox textBoxImei;
        private ToolTip tooltipBinaryBox;
        private TableLayoutPanel tableLayoutMain;
        private FlowLayoutPanel flowLayoutDownloadButtons;
        private TableLayoutPanel tableLayoutInfo;
        private TableLayoutPanel tableLayoutDownload;
        private TableLayoutPanel tableLayoutManual;

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
                this.comboBoxModel.Items.Clear();
                this.comboBoxModel.Items.AddRange(models);
            }
            this.comboBoxModel.Text = Settings.ReadSetting<string>("Model");
            string[] regions = Settings.ReadSetting<string[]>("Regions");
            if (regions?.Length > 0)
            {
                this.comboBoxRegion.Items.Clear();
                this.comboBoxRegion.Items.AddRange(regions);
            }
            this.comboBoxRegion.Text = Settings.ReadSetting<string>("Region");
            this.textBoxImei.Text = Settings.ReadSetting<string>("Imei");
            this.textBoxPda.Text = Settings.ReadSetting<string>("PDAVer");
            this.textBoxCsc.Text = Settings.ReadSetting<string>("CSCVer");
            this.textBoxPhone.Text = Settings.ReadSetting<string>("PHONEVer");
            if (Settings.ReadSetting<string>("AutoInfo").ToLower() == "true")
                this.checkBoxAuto.Checked = true;
            else
                this.checkBoxManual.Checked = true;
            if (Settings.ReadSetting<string>("SaveFileDialog").ToLower() == "false")
                this.saveFileDialogEnabled = false;
            if (Settings.ReadSetting<string>("BinaryNature").ToLower() == "true")
                this.checkBoxBinary.Checked = true;
            if (Settings.ReadSetting<string>("CheckCRC").ToLower() == "false")
                this.checkBoxCrc.Checked = false;
            if (Settings.ReadSetting<string>("AutoDecrypt").ToLower() == "false")
                this.checkBoxAutoDecrypt.Checked = false;
            this.tooltipBinary.SetToolTip(this.labelBinary, "Full firmware including PIT file");
            this.tooltipBinaryBox.SetToolTip(this.checkBoxBinary, "Full firmware including PIT file");
            Logger.WriteLog($"SamFirm v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}", false);
            ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => true;
        }

        private void Form1_Close(object sender, EventArgs e)
        {
            try
            {
                Settings.SetSetting("Model", this.comboBoxModel.Text.ToUpper());
                Settings.SetSetting("Region", this.comboBoxRegion.Text.ToUpper());
                Settings.SetSetting("Imei", this.textBoxImei.Text.ToUpper());
                Settings.SetSetting("PDAVer", this.textBoxPda.Text);
                Settings.SetSetting("CSCVer", this.textBoxCsc.Text);
                Settings.SetSetting("PHONEVer", this.textBoxPhone.Text);
                Settings.SetSetting("AutoInfo", this.checkBoxAuto.Checked.ToString());
                Settings.SetSetting("SaveFileDialog", this.saveFileDialogEnabled.ToString());
                Settings.SetSetting("BinaryNature", this.checkBoxBinary.Checked.ToString());
                Settings.SetSetting("CheckCRC", this.checkBoxCrc.Checked.ToString());
                Settings.SetSetting("AutoDecrypt", this.checkBoxAutoDecrypt.Checked.ToString());
            }
            catch { }
            this.PauseDownload = true;
            Thread.Sleep(100);
            Imports.FreeModule();
            Logger.SaveLog();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (this.buttonDownload.Text == "Pause")
            {
                Utility.TaskBarProgressState(true);
                this.PauseDownload = true;
                Utility.ReconnectDownload = false;
                this.buttonDownload.Text = "Download";
            }
            else
            {
                if (e is Form1.DownloadEventArgs downloadEventArgs && downloadEventArgs.isReconnect && (this.buttonDownload.Text == "Pause" || !Utility.ReconnectDownload))
                    return;
                if (this.PauseDownload)
                    Logger.WriteLog("Download thread is still running. Please wait.", false);
                else if (string.IsNullOrEmpty(this.textBoxFile.Text))
                {
                    Logger.WriteLog("No file to download. Please check for update first.", false);
                }
                else
                {
                    if (!(e is Form1.DownloadEventArgs args) || !args.isReconnect)
                    {
                        if (this.saveFileDialogEnabled)
                        {
                            string str = Path.GetExtension(Path.GetFileNameWithoutExtension(this.firmware.Filename)) + Path.GetExtension(this.firmware.Filename);
                            this.saveFileDialog.SupportMultiDottedExtensions = true;
                            this.saveFileDialog.OverwritePrompt = false;
                            this.saveFileDialog.FileName = this.firmware.Filename.Replace(str, "");
                            this.saveFileDialog.Filter = "Firmware|*" + str;
                            if (this.saveFileDialog.ShowDialog() != DialogResult.OK)
                            {
                                Logger.WriteLog("Aborted.", false);
                                return;
                            }
                            if (!this.saveFileDialog.FileName.EndsWith(str))
                                this.saveFileDialog.FileName += str;
                            else
                                this.saveFileDialog.FileName = this.saveFileDialog.FileName.Replace(str + str, str);
                            Logger.WriteLog($"Filename: {this.saveFileDialog.FileName}", false);
                            this.destinationFile = this.saveFileDialog.FileName;
                            if (File.Exists(this.destinationFile))
                            {
                                switch (new customMessageBox("The destination file already exists.\r\nWould you like to append it (resume download)?", "Append", DialogResult.Yes, "Overwrite", DialogResult.No, "Cancel", DialogResult.Cancel, SystemIcons.Warning.ToBitmap()).ShowDialog())
                                {
                                    case DialogResult.Cancel:
                                        Logger.WriteLog("Aborted.", false);
                                        return;
                                    case DialogResult.No:
                                        File.Delete(this.destinationFile);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            this.destinationFile = this.firmware.Filename;
                        }
                    }
                    Utility.TaskBarProgressState(false);
                    BackgroundWorker backgroundWorker = new BackgroundWorker();
                    backgroundWorker.DoWork += (o, _e) =>
                    {
                        try
                        {
                            this.ControlsEnabled(false);
                            Utility.ReconnectDownload = false;
                            this.buttonDownload.Invoke((Action)(() =>
                            {
                                this.buttonDownload.Enabled = true;
                                this.buttonDownload.Text = "Pause";
                            }));
                            if (this.firmware.Filename == this.destinationFile)
                                Logger.WriteLog($"Trying to download {this.firmware.Filename}", false);
                            else
                                Logger.WriteLog($"Trying to download {this.firmware.Filename} to {this.destinationFile}", false);
                            Command.Download(this.firmware.Path, this.firmware.Filename, this.firmware.Version, this.firmware.Region, this.firmware.Model_Type, this.destinationFile, this.firmware.Size, true);
                            if (this.PauseDownload)
                            {
                                Logger.WriteLog("Download paused", false);
                                this.PauseDownload = false;
                                if (Utility.ReconnectDownload)
                                {
                                    Logger.WriteLog("Reconnecting...", false);
                                    Utility.Reconnect(this.buttonDownload_Click);
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Download finished", false);
                                if (this.checkBoxCrc.Checked)
                                {
                                    if (this.firmware.CRC == null)
                                    {
                                        Logger.WriteLog("Unable to check CRC. Value not set by Samsung", false);
                                    }
                                    else
                                    {
                                        Logger.WriteLog("\nChecking CRC32...", false);
                                        if (!Utility.CRCCheck(this.destinationFile, this.firmware.CRC))
                                        {
                                            Logger.WriteLog("Error: CRC does not match. Please redownload the file.", false);
                                            File.Delete(this.destinationFile);
                                            if (!Utility.ReconnectDownload)
                                                this.ControlsEnabled(true);
                                            this.buttonDownload.Invoke((Action)(() => this.buttonDownload.Text = "Download"));
                                            return;
                                        }
                                        else
                                            Logger.WriteLog("Success: CRC match!", false);
                                    }
                                }
                                this.buttonDecrypt.Invoke((Action)(() => this.buttonDecrypt.Enabled = true));
                                if (this.checkBoxAutoDecrypt.Checked)
                                    this.buttonDecrypt_Click(o, null);
                            }
                            if (!Utility.ReconnectDownload)
                                this.ControlsEnabled(true);
                            this.buttonDownload.Invoke((Action)(() => this.buttonDownload.Text = "Download"));
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(ex.Message, false);
                            Logger.WriteLog(ex.ToString(), false);
                        }
                    };
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.comboBoxModel.Text))
                Logger.WriteLog("Error: Please specify a model", false);
            else if (string.IsNullOrEmpty(this.comboBoxRegion.Text))
                Logger.WriteLog("Error: Please specify a region", false);
            else if (string.IsNullOrEmpty(this.textBoxImei.Text))
                Logger.WriteLog("Error: Please specify an Imei or Serial number", false);
            else if (this.checkBoxManual.Checked && (string.IsNullOrEmpty(this.textBoxImei.Text) || string.IsNullOrEmpty(this.textBoxPda.Text) || string.IsNullOrEmpty(this.textBoxCsc.Text) || string.IsNullOrEmpty(this.textBoxPhone.Text)))
            {
                Logger.WriteLog("Error: Please specify PDA, CSC and Phone version and Imei/Serial or use Auto Method", false);
            }
            else
            {
                string model = this.comboBoxModel.Text.ToUpper();
                string region = this.comboBoxRegion.Text.ToUpper();
                string imei = this.textBoxImei.Text.ToUpper();
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (o, _e) =>
                {
                    try
                    {
                        this.SetProgressBar(0, 0);
                        this.ControlsEnabled(false);
                        Utility.ReconnectDownload = false;
                        this.firmware = !this.checkBoxAuto.Checked ? Command.UpdateCheck(model, region, imei, this.textBoxPda.Text, this.textBoxCsc.Text, this.textBoxPhone.Text, this.textBoxPda.Text, this.checkBoxBinary.Checked, false) : Command.UpdateCheckAuto(model, region, imei, this.checkBoxBinary.Checked);
                        if (!string.IsNullOrEmpty(this.firmware.Filename))
                        {
                            this.textBoxFile.Invoke((Action)(() => this.textBoxFile.Text = this.firmware.Filename));
                            this.textBoxVersion.Invoke((Action)(() => this.textBoxVersion.Text = this.firmware.Version));
                            this.textBoxSize.Invoke((Action)(() => this.textBoxSize.Text = $"{(long.Parse(this.firmware.Size) / 1024L / 1024L)} MB"));
                            this.comboBoxModel.Invoke((Action)(() =>
                            {
                                var items = comboBoxModel.Items.OfType<string>().ToList();
                                items.Add(model);
                                Settings.SetSetting("Models", items.Distinct().OrderBy(s => s));
                                items = comboBoxRegion.Items.OfType<string>().ToList();
                                items.Add(region);
                                Settings.SetSetting("Regions", items.Distinct().OrderBy(s => s));
                            }));
                        }
                        else
                        {
                            this.textBoxFile.Invoke((Action)(() => this.textBoxFile.Text = string.Empty));
                            this.textBoxVersion.Invoke((Action)(() => this.textBoxVersion.Text = string.Empty));
                            this.textBoxSize.Invoke((Action)(() => this.textBoxSize.Text = string.Empty));
                        }
                        this.ControlsEnabled(true);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex.Message, false);
                        Logger.WriteLog(ex.ToString(), false);
                    }
                };
                backgroundWorker.RunWorkerAsync();
            }
        }

        public void SetProgressBar(int Progress, long bytesTransferred)
        {
            if (Progress > 100)
                Progress = 100;
            this.progressBar.Invoke((Action)(() =>
            {
                this.progressBar.Value = Progress;
                if (bytesTransferred > 0)
                {
                    this.labelTransferred.Text = $"{bytesTransferred / 1024.0 / 1024.0:0.00} MB";
                }
                else
                {
                    this.labelTransferred.Text = "";
                }
                try
                {
                    TaskbarManager.Instance.SetProgressValue(Progress, 100);
                }
                catch (Exception)
                {
                }
            }));
        }

        private void ControlsEnabled(bool enabled)
        {
            this.buttonUpdate.Invoke((Action)(() => this.buttonUpdate.Enabled = enabled));
            this.buttonDownload.Invoke((Action)(() => this.buttonDownload.Enabled = enabled));
            this.checkBoxBinary.Invoke((Action)(() => this.checkBoxBinary.Enabled = enabled));
            this.comboBoxModel.Invoke((Action)(() => this.comboBoxModel.Enabled = enabled));
            this.comboBoxRegion.Invoke((Action)(() => this.comboBoxRegion.Enabled = enabled));
            this.checkBoxAuto.Invoke((Action)(() => this.checkBoxAuto.Enabled = enabled));
            this.checkBoxManual.Invoke((Action)(() => this.checkBoxManual.Enabled = enabled));
            this.checkBoxManual.Invoke((Action)(() =>
            {
                if (!this.checkBoxManual.Checked)
                    return;
                this.textBoxPda.Enabled = enabled;
                this.textBoxCsc.Enabled = enabled;
                this.textBoxPhone.Enabled = enabled;
            }));
            this.checkBoxAutoDecrypt.Invoke((Action)(() => this.checkBoxAutoDecrypt.Enabled = enabled));
            this.checkBoxCrc.Invoke((Action)(() => this.checkBoxCrc.Enabled = enabled));
        }

        private void buttonDecrypt_Click(object sender, EventArgs e)
        {
            if (!File.Exists(this.destinationFile))
            {
                Logger.WriteLog($"Error: File {this.destinationFile} does not exist", false);
            }
            else
            {
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (o, _e) =>
                {
                    Thread.Sleep(100);
                    Logger.WriteLog("\nDecrypting and unzipping firmware...", false);
                    this.ControlsEnabled(false);
                    this.buttonDecrypt.Invoke((Action)(() => this.buttonDecrypt.Enabled = false));
                    if (this.destinationFile.EndsWith(".enc2"))
                    {
                        Crypto.SetDecryptKey(this.firmware.Region, this.firmware.Model, this.firmware.Version);
                    }
                    else if (this.destinationFile.EndsWith(".enc4"))
                    {
                        if (this.firmware.BinaryNature == 1)
                            Crypto.SetDecryptKey(this.firmware.Version, this.firmware.LogicValueFactory);
                        else
                            Crypto.SetDecryptKey(this.firmware.Version, this.firmware.LogicValueHome);
                    }
                    string outputDirectory = Path.Combine(Path.GetDirectoryName(this.destinationFile), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(this.destinationFile)));
                    if (Crypto.DecryptAndUnzip(this.destinationFile, outputDirectory, true) == 0)
                    {
                        CmdLine.SaveMeta(this.firmware, Path.Combine(outputDirectory, "FirmwareInfo.txt"));
                        File.Delete(this.destinationFile);
                    }
                    Logger.WriteLog("Decryption finished", false);
                    this.ControlsEnabled(true);
                };
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void checkBoxManual_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkBoxAuto.Checked && !this.checkBoxManual.Checked)
            {
                this.checkBoxManual.Checked = true;
            }
            else
            {
                this.checkBoxAuto.Checked = !this.checkBoxManual.Checked;
                this.textBoxPda.Enabled = this.checkBoxManual.Checked;
                this.textBoxCsc.Enabled = this.checkBoxManual.Checked;
                this.textBoxPhone.Enabled = this.checkBoxManual.Checked;
            }
        }

        private void checkBoxAuto_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkBoxManual.Checked && !this.checkBoxAuto.Checked)
            {
                this.checkBoxAuto.Checked = true;
            }
            else
            {
                this.checkBoxManual.Checked = !this.checkBoxAuto.Checked;
                this.textBoxPda.Enabled = !this.checkBoxAuto.Checked;
                this.textBoxCsc.Enabled = !this.checkBoxAuto.Checked;
                this.textBoxPhone.Enabled = !this.checkBoxAuto.Checked;
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
            this.comboBoxModel = new System.Windows.Forms.ComboBox();
            this.labelModel = new System.Windows.Forms.Label();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.labelRegion = new System.Windows.Forms.Label();
            this.comboBoxRegion = new System.Windows.Forms.ComboBox();
            this.labelPda = new System.Windows.Forms.Label();
            this.textBoxPda = new System.Windows.Forms.TextBox();
            this.labelCsc = new System.Windows.Forms.Label();
            this.textBoxCsc = new System.Windows.Forms.TextBox();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.labelPhone = new System.Windows.Forms.Label();
            this.textBoxPhone = new System.Windows.Forms.TextBox();
            this.labelFile = new System.Windows.Forms.Label();
            this.textBoxFile = new System.Windows.Forms.TextBox();
            this.labelVersion = new System.Windows.Forms.Label();
            this.textBoxVersion = new System.Windows.Forms.TextBox();
            this.groupBoxInfo = new System.Windows.Forms.GroupBox();
            this.labelImei = new System.Windows.Forms.Label();
            this.textBoxImei = new System.Windows.Forms.TextBox();
            this.groupBoxManual = new System.Windows.Forms.GroupBox();
            this.checkBoxManual = new System.Windows.Forms.CheckBox();
            this.checkBoxAuto = new System.Windows.Forms.CheckBox();
            this.checkBoxBinary = new System.Windows.Forms.CheckBox();
            this.labelBinary = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonDecrypt = new System.Windows.Forms.Button();
            this.groupBoxDownload = new System.Windows.Forms.GroupBox();
            this.labelTransferred = new System.Windows.Forms.Label();
            this.labelSpeedTitle = new System.Windows.Forms.Label();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.checkBoxAutoDecrypt = new System.Windows.Forms.CheckBox();
            this.checkBoxCrc = new System.Windows.Forms.CheckBox();
            this.textBoxSize = new System.Windows.Forms.TextBox();
            this.labelSize = new System.Windows.Forms.Label();
            this.tooltipBinary = new System.Windows.Forms.ToolTip(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.tooltipBinaryBox = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutInfo = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutDownload = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutDownloadButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutManual = new System.Windows.Forms.TableLayoutPanel();
            this.groupBoxInfo.SuspendLayout();
            this.groupBoxManual.SuspendLayout();
            this.groupBoxDownload.SuspendLayout();
            this.tableLayoutMain.SuspendLayout();
            this.tableLayoutInfo.SuspendLayout();
            this.tableLayoutDownload.SuspendLayout();
            this.flowLayoutDownloadButtons.SuspendLayout();
            this.tableLayoutManual.SuspendLayout();
            this.SuspendLayout();
            //
            // tableLayoutMain
            //
            this.tableLayoutMain.ColumnCount = 2;
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutMain.Controls.Add(this.groupBoxInfo, 0, 0);
            this.tableLayoutMain.Controls.Add(this.groupBoxDownload, 1, 0);
            this.tableLayoutMain.Controls.Add(this.richTextBoxLog, 0, 1);
            this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(10);
            this.tableLayoutMain.RowCount = 2;
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.Size = new System.Drawing.Size(900, 600);
            this.tableLayoutMain.TabIndex = 0;
            //
            // groupBoxInfo
            //
            this.groupBoxInfo.Controls.Add(this.tableLayoutInfo);
            this.groupBoxInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupBoxInfo.Location = new System.Drawing.Point(13, 13);
            this.groupBoxInfo.Name = "groupBoxInfo";
            this.groupBoxInfo.Size = new System.Drawing.Size(390, 344);
            this.groupBoxInfo.TabIndex = 0;
            this.groupBoxInfo.TabStop = false;
            this.groupBoxInfo.Text = "Firmware Info";
            //
            // tableLayoutInfo
            //
            this.tableLayoutInfo.ColumnCount = 2;
            this.tableLayoutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutInfo.Controls.Add(this.labelModel, 0, 0);
            this.tableLayoutInfo.Controls.Add(this.comboBoxModel, 1, 0);
            this.tableLayoutInfo.Controls.Add(this.labelRegion, 0, 1);
            this.tableLayoutInfo.Controls.Add(this.comboBoxRegion, 1, 1);
            this.tableLayoutInfo.Controls.Add(this.labelImei, 0, 2);
            this.tableLayoutInfo.Controls.Add(this.textBoxImei, 1, 2);
            this.tableLayoutInfo.Controls.Add(this.checkBoxAuto, 0, 3);
            this.tableLayoutInfo.Controls.Add(this.checkBoxManual, 1, 3);
            this.tableLayoutInfo.Controls.Add(this.groupBoxManual, 0, 4);
            this.tableLayoutInfo.Controls.Add(this.labelBinary, 0, 5);
            this.tableLayoutInfo.Controls.Add(this.checkBoxBinary, 1, 5);
            this.tableLayoutInfo.Controls.Add(this.buttonUpdate, 1, 6);
            this.tableLayoutInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutInfo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.tableLayoutInfo.Location = new System.Drawing.Point(3, 19);
            this.tableLayoutInfo.Name = "tableLayoutInfo";
            this.tableLayoutInfo.RowCount = 7;
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutInfo.Size = new System.Drawing.Size(384, 322);
            this.tableLayoutInfo.TabIndex = 0;
            //
            // labelModel
            //
            this.labelModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelModel.AutoSize = true;
            this.labelModel.Location = new System.Drawing.Point(3, 7);
            this.labelModel.Name = "labelModel";
            this.labelModel.Size = new System.Drawing.Size(41, 15);
            this.labelModel.TabIndex = 0;
            this.labelModel.Text = "Model";
            //
            // comboBoxModel
            //
            this.comboBoxModel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxModel.FormattingEnabled = true;
            this.comboBoxModel.Items.AddRange(new object[] {
            "SM-G960F",
            "SM-G973F",
            "SM-S901B",
            "SM-G991B",
            "SM-G998B",
            "SM-S911B",
            "SM-T395",
            "SM-T545",
            "SM-T575"});
            this.comboBoxModel.Location = new System.Drawing.Point(103, 3);
            this.comboBoxModel.Name = "comboBoxModel";
            this.comboBoxModel.Size = new System.Drawing.Size(278, 23);
            this.comboBoxModel.TabIndex = 0;
            //
            // labelRegion
            //
            this.labelRegion.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelRegion.AutoSize = true;
            this.labelRegion.Location = new System.Drawing.Point(3, 37);
            this.labelRegion.Name = "labelRegion";
            this.labelRegion.Size = new System.Drawing.Size(44, 15);
            this.labelRegion.TabIndex = 2;
            this.labelRegion.Text = "Region";
            //
            // comboBoxRegion
            //
            this.comboBoxRegion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Items.AddRange(new object[] {
            "NEE",
            "EUX"});
            this.comboBoxRegion.Location = new System.Drawing.Point(103, 33);
            this.comboBoxRegion.Name = "comboBoxRegion";
            this.comboBoxRegion.Size = new System.Drawing.Size(278, 23);
            this.comboBoxRegion.TabIndex = 1;
            //
            // labelImei
            //
            this.labelImei.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelImei.AutoSize = true;
            this.labelImei.Location = new System.Drawing.Point(3, 67);
            this.labelImei.Name = "labelImei";
            this.labelImei.Size = new System.Drawing.Size(63, 15);
            this.labelImei.TabIndex = 4;
            this.labelImei.Text = "Imei/Serial";
            this.labelImei.Click += new System.EventHandler(this.labelImei_Click);
            //
            // textBoxImei
            //
            this.textBoxImei.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxImei.Location = new System.Drawing.Point(103, 63);
            this.textBoxImei.Name = "textBoxImei";
            this.textBoxImei.Size = new System.Drawing.Size(278, 23);
            this.textBoxImei.TabIndex = 2;
            //
            // checkBoxAuto
            //
            this.checkBoxAuto.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxAuto.AutoSize = true;
            this.checkBoxAuto.Location = new System.Drawing.Point(3, 95);
            this.checkBoxAuto.Name = "checkBoxAuto";
            this.checkBoxAuto.Size = new System.Drawing.Size(52, 19);
            this.checkBoxAuto.TabIndex = 3;
            this.checkBoxAuto.Text = "Auto";
            this.checkBoxAuto.UseVisualStyleBackColor = true;
            this.checkBoxAuto.CheckedChanged += new System.EventHandler(this.checkBoxAuto_CheckedChanged);
            //
            // checkBoxManual
            //
            this.checkBoxManual.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxManual.AutoSize = true;
            this.checkBoxManual.Location = new System.Drawing.Point(103, 95);
            this.checkBoxManual.Name = "checkBoxManual";
            this.checkBoxManual.Size = new System.Drawing.Size(66, 19);
            this.checkBoxManual.TabIndex = 4;
            this.checkBoxManual.Text = "Manual";
            this.checkBoxManual.UseVisualStyleBackColor = true;
            this.checkBoxManual.CheckedChanged += new System.EventHandler(this.checkBoxManual_CheckedChanged);
            //
            // groupBoxManual
            //
            this.tableLayoutInfo.SetColumnSpan(this.groupBoxManual, 2);
            this.groupBoxManual.Controls.Add(this.tableLayoutManual);
            this.groupBoxManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxManual.Location = new System.Drawing.Point(3, 123);
            this.groupBoxManual.Name = "groupBoxManual";
            this.groupBoxManual.Size = new System.Drawing.Size(378, 104);
            this.groupBoxManual.TabIndex = 8;
            this.groupBoxManual.TabStop = false;
            //
            // tableLayoutManual
            //
            this.tableLayoutManual.ColumnCount = 2;
            this.tableLayoutManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutManual.Controls.Add(this.labelPda, 0, 0);
            this.tableLayoutManual.Controls.Add(this.textBoxPda, 1, 0);
            this.tableLayoutManual.Controls.Add(this.labelCsc, 0, 1);
            this.tableLayoutManual.Controls.Add(this.textBoxCsc, 1, 1);
            this.tableLayoutManual.Controls.Add(this.labelPhone, 0, 2);
            this.tableLayoutManual.Controls.Add(this.textBoxPhone, 1, 2);
            this.tableLayoutManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutManual.Location = new System.Drawing.Point(3, 19);
            this.tableLayoutManual.Name = "tableLayoutManual";
            this.tableLayoutManual.RowCount = 3;
            this.tableLayoutManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutManual.Size = new System.Drawing.Size(372, 82);
            this.tableLayoutManual.TabIndex = 0;
            //
            // labelPda
            //
            this.labelPda.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelPda.AutoSize = true;
            this.labelPda.Location = new System.Drawing.Point(3, 6);
            this.labelPda.Name = "labelPda";
            this.labelPda.Size = new System.Drawing.Size(30, 15);
            this.labelPda.TabIndex = 0;
            this.labelPda.Text = "PDA";
            //
            // textBoxPda
            //
            this.textBoxPda.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxPda.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxPda.Location = new System.Drawing.Point(97, 3);
            this.textBoxPda.Name = "textBoxPda";
            this.textBoxPda.Size = new System.Drawing.Size(272, 23);
            this.textBoxPda.TabIndex = 5;
            //
            // labelCsc
            //
            this.labelCsc.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelCsc.AutoSize = true;
            this.labelCsc.Location = new System.Drawing.Point(3, 34);
            this.labelCsc.Name = "labelCsc";
            this.labelCsc.Size = new System.Drawing.Size(29, 15);
            this.labelCsc.TabIndex = 2;
            this.labelCsc.Text = "CSC";
            //
            // textBoxCsc
            //
            this.textBoxCsc.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxCsc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxCsc.Location = new System.Drawing.Point(97, 31);
            this.textBoxCsc.Name = "textBoxCsc";
            this.textBoxCsc.Size = new System.Drawing.Size(272, 23);
            this.textBoxCsc.TabIndex = 6;
            //
            // labelPhone
            //
            this.labelPhone.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelPhone.AutoSize = true;
            this.labelPhone.Location = new System.Drawing.Point(3, 62);
            this.labelPhone.Name = "labelPhone";
            this.labelPhone.Size = new System.Drawing.Size(41, 15);
            this.labelPhone.TabIndex = 4;
            this.labelPhone.Text = "Phone";
            //
            // textBoxPhone
            //
            this.textBoxPhone.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxPhone.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxPhone.Location = new System.Drawing.Point(97, 59);
            this.textBoxPhone.Name = "textBoxPhone";
            this.textBoxPhone.Size = new System.Drawing.Size(272, 23);
            this.textBoxPhone.TabIndex = 7;
            //
            // labelBinary
            //
            this.labelBinary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelBinary.AutoSize = true;
            this.labelBinary.Location = new System.Drawing.Point(3, 237);
            this.labelBinary.Name = "labelBinary";
            this.labelBinary.Size = new System.Drawing.Size(79, 15);
            this.labelBinary.TabIndex = 9;
            this.labelBinary.Text = "Binary Nature";
            //
            // checkBoxBinary
            //
            this.checkBoxBinary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxBinary.AutoSize = true;
            this.checkBoxBinary.Location = new System.Drawing.Point(103, 238);
            this.checkBoxBinary.Name = "checkBoxBinary";
            this.checkBoxBinary.Size = new System.Drawing.Size(15, 14);
            this.checkBoxBinary.TabIndex = 9;
            this.checkBoxBinary.UseVisualStyleBackColor = true;
            //
            // buttonUpdate
            //
            this.buttonUpdate.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.buttonUpdate.Location = new System.Drawing.Point(251, 276);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(130, 30);
            this.buttonUpdate.TabIndex = 10;
            this.buttonUpdate.Text = "Check Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
            //
            // groupBoxDownload
            //
            this.groupBoxDownload.Controls.Add(this.tableLayoutDownload);
            this.groupBoxDownload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxDownload.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupBoxDownload.Location = new System.Drawing.Point(409, 13);
            this.groupBoxDownload.Name = "groupBoxDownload";
            this.groupBoxDownload.Size = new System.Drawing.Size(478, 344);
            this.groupBoxDownload.TabIndex = 1;
            this.groupBoxDownload.TabStop = false;
            this.groupBoxDownload.Text = "Download";
            //
            // tableLayoutDownload
            //
            this.tableLayoutDownload.ColumnCount = 2;
            this.tableLayoutDownload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutDownload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutDownload.Controls.Add(this.labelFile, 0, 0);
            this.tableLayoutDownload.Controls.Add(this.textBoxFile, 1, 0);
            this.tableLayoutDownload.Controls.Add(this.labelVersion, 0, 1);
            this.tableLayoutDownload.Controls.Add(this.textBoxVersion, 1, 1);
            this.tableLayoutDownload.Controls.Add(this.labelSize, 0, 2);
            this.tableLayoutDownload.Controls.Add(this.textBoxSize, 1, 2);
            this.tableLayoutDownload.Controls.Add(this.checkBoxCrc, 1, 3);
            this.tableLayoutDownload.Controls.Add(this.checkBoxAutoDecrypt, 1, 4);
            this.tableLayoutDownload.Controls.Add(this.flowLayoutDownloadButtons, 1, 5);
            this.tableLayoutDownload.Controls.Add(this.progressBar, 1, 6);
            this.tableLayoutDownload.Controls.Add(this.labelSpeedTitle, 0, 7);
            this.tableLayoutDownload.Controls.Add(this.labelSpeed, 1, 7);
            this.tableLayoutDownload.Controls.Add(this.labelTransferred, 1, 8);
            this.tableLayoutDownload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutDownload.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.tableLayoutDownload.Location = new System.Drawing.Point(3, 19);
            this.tableLayoutDownload.Name = "tableLayoutDownload";
            this.tableLayoutDownload.RowCount = 9;
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutDownload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutDownload.Size = new System.Drawing.Size(472, 322);
            this.tableLayoutDownload.TabIndex = 0;
            //
            // labelFile
            //
            this.labelFile.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelFile.AutoSize = true;
            this.labelFile.Location = new System.Drawing.Point(3, 7);
            this.labelFile.Name = "labelFile";
            this.labelFile.Size = new System.Drawing.Size(25, 15);
            this.labelFile.TabIndex = 0;
            this.labelFile.Text = "File";
            //
            // textBoxFile
            //
            this.textBoxFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFile.Location = new System.Drawing.Point(83, 3);
            this.textBoxFile.Name = "textBoxFile";
            this.textBoxFile.ReadOnly = true;
            this.textBoxFile.Size = new System.Drawing.Size(386, 23);
            this.textBoxFile.TabIndex = 11;
            //
            // labelVersion
            //
            this.labelVersion.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(3, 37);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(45, 15);
            this.labelVersion.TabIndex = 2;
            this.labelVersion.Text = "Version";
            //
            // textBoxVersion
            //
            this.textBoxVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxVersion.Location = new System.Drawing.Point(83, 33);
            this.textBoxVersion.Name = "textBoxVersion";
            this.textBoxVersion.ReadOnly = true;
            this.textBoxVersion.Size = new System.Drawing.Size(386, 23);
            this.textBoxVersion.TabIndex = 12;
            //
            // labelSize
            //
            this.labelSize.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelSize.AutoSize = true;
            this.labelSize.Location = new System.Drawing.Point(3, 67);
            this.labelSize.Name = "labelSize";
            this.labelSize.Size = new System.Drawing.Size(27, 15);
            this.labelSize.TabIndex = 4;
            this.labelSize.Text = "Size";
            //
            // textBoxSize
            //
            this.textBoxSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSize.Location = new System.Drawing.Point(83, 63);
            this.textBoxSize.Name = "textBoxSize";
            this.textBoxSize.ReadOnly = true;
            this.textBoxSize.Size = new System.Drawing.Size(386, 23);
            this.textBoxSize.TabIndex = 13;
            //
            // checkBoxCrc
            //
            this.checkBoxCrc.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxCrc.AutoSize = true;
            this.checkBoxCrc.Checked = true;
            this.checkBoxCrc.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCrc.Location = new System.Drawing.Point(83, 95);
            this.checkBoxCrc.Name = "checkBoxCrc";
            this.checkBoxCrc.Size = new System.Drawing.Size(97, 19);
            this.checkBoxCrc.TabIndex = 14;
            this.checkBoxCrc.Text = "Check CRC32";
            this.checkBoxCrc.UseVisualStyleBackColor = true;
            //
            // checkBoxAutoDecrypt
            //
            this.checkBoxAutoDecrypt.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxAutoDecrypt.AutoSize = true;
            this.checkBoxAutoDecrypt.Checked = true;
            this.checkBoxAutoDecrypt.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoDecrypt.Location = new System.Drawing.Point(83, 125);
            this.checkBoxAutoDecrypt.Name = "checkBoxAutoDecrypt";
            this.checkBoxAutoDecrypt.Size = new System.Drawing.Size(143, 19);
            this.checkBoxAutoDecrypt.TabIndex = 15;
            this.checkBoxAutoDecrypt.Text = "Decrypt automatically";
            this.checkBoxAutoDecrypt.UseVisualStyleBackColor = true;
            //
            // flowLayoutDownloadButtons
            //
            this.flowLayoutDownloadButtons.Controls.Add(this.buttonDownload);
            this.flowLayoutDownloadButtons.Controls.Add(this.buttonDecrypt);
            this.flowLayoutDownloadButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutDownloadButtons.Location = new System.Drawing.Point(80, 150);
            this.flowLayoutDownloadButtons.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutDownloadButtons.Name = "flowLayoutDownloadButtons";
            this.flowLayoutDownloadButtons.Size = new System.Drawing.Size(392, 40);
            this.flowLayoutDownloadButtons.TabIndex = 16;
            //
            // buttonDownload
            //
            this.buttonDownload.Location = new System.Drawing.Point(3, 3);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(100, 30);
            this.buttonDownload.TabIndex = 16;
            this.buttonDownload.Text = "Download";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            //
            // buttonDecrypt
            //
            this.buttonDecrypt.Enabled = false;
            this.buttonDecrypt.Location = new System.Drawing.Point(109, 3);
            this.buttonDecrypt.Name = "buttonDecrypt";
            this.buttonDecrypt.Size = new System.Drawing.Size(100, 30);
            this.buttonDecrypt.TabIndex = 17;
            this.buttonDecrypt.Text = "Decrypt";
            this.buttonDecrypt.UseVisualStyleBackColor = true;
            this.buttonDecrypt.Click += new System.EventHandler(this.buttonDecrypt_Click);
            //
            // progressBar
            //
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Location = new System.Drawing.Point(83, 193);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(386, 29);
            this.progressBar.TabIndex = 18;
            //
            // labelSpeedTitle
            //
            this.labelSpeedTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelSpeedTitle.AutoSize = true;
            this.labelSpeedTitle.Location = new System.Drawing.Point(3, 230);
            this.labelSpeedTitle.Name = "labelSpeedTitle";
            this.labelSpeedTitle.Size = new System.Drawing.Size(39, 15);
            this.labelSpeedTitle.TabIndex = 19;
            this.labelSpeedTitle.Text = "Speed";
            //
            // labelSpeed
            //
            this.labelSpeed.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.Location = new System.Drawing.Point(83, 230);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(38, 15);
            this.labelSpeed.TabIndex = 20;
            this.labelSpeed.Text = "0 KB/s";
            //
            // labelTransferred
            //
            this.labelTransferred.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelTransferred.AutoSize = true;
            this.labelTransferred.Location = new System.Drawing.Point(83, 273);
            this.labelTransferred.Name = "labelTransferred";
            this.labelTransferred.Size = new System.Drawing.Size(35, 15);
            this.labelTransferred.TabIndex = 21;
            this.labelTransferred.Text = "0 MB";
            //
            // richTextBoxLog
            //
            this.tableLayoutMain.SetColumnSpan(this.richTextBoxLog, 2);
            this.richTextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.richTextBoxLog.Location = new System.Drawing.Point(13, 363);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.ReadOnly = true;
            this.richTextBoxLog.Size = new System.Drawing.Size(874, 224);
            this.richTextBoxLog.TabIndex = 22;
            this.richTextBoxLog.Text = "";
            //
            // saveFileDialog
            //
            this.saveFileDialog.SupportMultiDottedExtensions = true;
            //
            // Form1
            //
            this.AcceptButton = this.buttonUpdate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.tableLayoutMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "SamFirm Reborn";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Close);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBoxInfo.ResumeLayout(false);
            this.groupBoxManual.ResumeLayout(false);
            this.groupBoxDownload.ResumeLayout(false);
            this.tableLayoutMain.ResumeLayout(false);
            this.tableLayoutInfo.ResumeLayout(false);
            this.tableLayoutInfo.PerformLayout();
            this.tableLayoutDownload.ResumeLayout(false);
            this.tableLayoutDownload.PerformLayout();
            this.flowLayoutDownloadButtons.ResumeLayout(false);
            this.tableLayoutManual.ResumeLayout(false);
            this.tableLayoutManual.PerformLayout();
            this.ResumeLayout(false);
        }

        public class DownloadEventArgs : EventArgs
        {
            public bool isReconnect;
        }

        private void labelImei_Click(object sender, EventArgs e)
        {
        }
    }
}
