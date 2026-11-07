using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Cw4French
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Environment.Exit(InstallerEngine.RunCli(args));
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly TextBox directoryBox;
        private readonly Button browseButton;
        private readonly Button installButton;
        private readonly Button restoreButton;
        private readonly Label statusLabel;
        private readonly Label detailLabel;
        private readonly ProgressBar progressBar;
        private readonly BackgroundWorker worker;

        private enum Operation
        {
            Install,
            Restore
        }

        public InstallerForm()
        {
            Text = "Creeper World 4 — Patch français";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(740, 440);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            BackColor = Color.FromArgb(246, 248, 251);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 104;
            header.BackColor = Color.FromArgb(27, 45, 69);
            Controls.Add(header);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Creeper World 4";
            title.Font = new Font("Segoe UI", 21F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.White;
            title.Location = new Point(30, 17);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.AutoSize = true;
            subtitle.Text = "Installateur du patch français relu";
            subtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.ForeColor = Color.FromArgb(208, 220, 234);
            subtitle.Location = new Point(33, 63);
            header.Controls.Add(subtitle);

            Panel content = new Panel();
            content.Location = new Point(0, 104);
            content.Size = new Size(740, 309);
            content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            content.Padding = new Padding(30, 19, 30, 10);
            Controls.Add(content);

            Label folderCaption = new Label();
            folderCaption.AutoSize = true;
            folderCaption.Text = "Dossier du jeu";
            folderCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            folderCaption.ForeColor = Color.FromArgb(43, 56, 73);
            folderCaption.Location = new Point(30, 15);
            content.Controls.Add(folderCaption);

            Label folderHelp = new Label();
            folderHelp.AutoSize = true;
            folderHelp.Text = "Choisissez le dossier qui contient CW4.exe.";
            folderHelp.ForeColor = Color.FromArgb(99, 112, 128);
            folderHelp.Location = new Point(30, 39);
            content.Controls.Add(folderHelp);

            directoryBox = new TextBox();
            directoryBox.Location = new Point(30, 67);
            directoryBox.Size = new Size(526, 27);
            directoryBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            directoryBox.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            directoryBox.AccessibleName = "Dossier du jeu";
            content.Controls.Add(directoryBox);

            browseButton = new Button();
            browseButton.Text = "Parcourir…";
            browseButton.Location = new Point(567, 65);
            browseButton.Size = new Size(111, 30);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.FlatStyle = FlatStyle.System;
            browseButton.Click += BrowseButton_Click;
            content.Controls.Add(browseButton);

            Panel compatibility = new Panel();
            compatibility.Location = new Point(30, 99);
            compatibility.Size = new Size(648, 42);
            compatibility.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            compatibility.BackColor = Color.FromArgb(232, 240, 248);
            content.Controls.Add(compatibility);

            Label compatibilityText = new Label();
            compatibilityText.AutoSize = false;
            compatibilityText.Dock = DockStyle.Fill;
            compatibilityText.TextAlign = ContentAlignment.MiddleLeft;
            compatibilityText.Padding = new Padding(12, 0, 8, 0);
            compatibilityText.Text = "Compatibilité : Creeper World 4 — version 2.5.1, fichiers vérifiés";
            compatibilityText.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            compatibilityText.ForeColor = Color.FromArgb(48, 77, 106);
            compatibility.Controls.Add(compatibilityText);

            Label explanation = new Label();
            explanation.AutoSize = false;
            explanation.Location = new Point(30, 151);
            explanation.Size = new Size(648, 42);
            explanation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            explanation.Text = "L’installation vérifie la version du jeu et conserve une copie des fichiers anglais.\r\nVous pourrez les restaurer à tout moment avec le bouton ci-dessous.";
            explanation.ForeColor = Color.FromArgb(77, 89, 105);
            explanation.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            content.Controls.Add(explanation);

            installButton = new Button();
            installButton.Text = "Installer le français";
            installButton.Location = new Point(30, 196);
            installButton.Size = new Size(206, 40);
            installButton.BackColor = Color.FromArgb(35, 113, 179);
            installButton.ForeColor = Color.White;
            installButton.FlatStyle = FlatStyle.Flat;
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            installButton.Click += InstallButton_Click;
            content.Controls.Add(installButton);

            restoreButton = new Button();
            restoreButton.Text = "Restaurer l’anglais";
            restoreButton.Location = new Point(248, 196);
            restoreButton.Size = new Size(206, 40);
            restoreButton.BackColor = Color.FromArgb(231, 236, 242);
            restoreButton.ForeColor = Color.FromArgb(43, 56, 73);
            restoreButton.FlatStyle = FlatStyle.Flat;
            restoreButton.FlatAppearance.BorderColor = Color.FromArgb(202, 211, 222);
            restoreButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            restoreButton.Click += RestoreButton_Click;
            content.Controls.Add(restoreButton);

            Panel statusPanel = new Panel();
            statusPanel.Location = new Point(30, 246);
            statusPanel.Size = new Size(648, 62);
            statusPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            statusPanel.BackColor = Color.White;
            statusPanel.BorderStyle = BorderStyle.FixedSingle;
            content.Controls.Add(statusPanel);

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.Location = new Point(13, 5);
            statusLabel.Size = new Size(606, 18);
            statusLabel.Text = "Prêt. Sélectionnez le dossier du jeu pour commencer.";
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            statusLabel.ForeColor = Color.FromArgb(43, 56, 73);
            statusPanel.Controls.Add(statusLabel);

            detailLabel = new Label();
            detailLabel.AutoSize = false;
            detailLabel.Location = new Point(13, 23);
            detailLabel.Size = new Size(606, 15);
            detailLabel.Text = "";
            detailLabel.ForeColor = Color.FromArgb(99, 112, 128);
            statusPanel.Controls.Add(detailLabel);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(13, 42);
            progressBar.Size = new Size(606, 10);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            statusPanel.Controls.Add(progressBar);

            Label footer = new Label();
            footer.AutoSize = false;
            footer.Dock = DockStyle.Bottom;
            footer.Height = 27;
            footer.Text = "Après l’opération, relancez le jeu pour afficher la langue choisie.";
            footer.TextAlign = ContentAlignment.MiddleLeft;
            footer.ForeColor = Color.FromArgb(99, 112, 128);
            footer.Padding = new Padding(30, 0, 0, 0);
            footer.BackColor = Color.FromArgb(246, 248, 251);
            Controls.Add(footer);

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            Load += InstallerForm_Load;
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (worker.IsBusy) {
                    e.Cancel = true;
                    MessageBox.Show(this, "Veuillez attendre la fin de l’opération avant de fermer l’installateur.", "Opération en cours", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        private void InstallerForm_Load(object sender, EventArgs e)
        {
            try
            {
                string found = InstallerEngine.FindGameDirectory();
                if (!String.IsNullOrEmpty(found))
                    directoryBox.Text = found;
            }
            catch
            {
                // La détection automatique est facultative; l’utilisateur peut choisir le dossier.
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionnez le dossier d’installation de Creeper World 4.";
                dialog.ShowNewFolderButton = false;
                if (Directory.Exists(directoryBox.Text))
                    dialog.SelectedPath = directoryBox.Text;
                else
                    dialog.SelectedPath = Application.StartupPath;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    directoryBox.Text = dialog.SelectedPath;
            }
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            StartOperation(Operation.Install);
        }

        private void RestoreButton_Click(object sender, EventArgs e)
        {
            StartOperation(Operation.Restore);
        }

        private void StartOperation(Operation operation)
        {
            string directory = directoryBox.Text.Trim();
            if (directory.Length == 0)
            {
                MessageBox.Show(this, "Sélectionnez le dossier de Creeper World 4 avant de continuer.",
                    "Dossier du jeu requis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                directoryBox.Focus();
                return;
            }

            if (worker.IsBusy)
                return;

            installButton.Enabled = false;
            restoreButton.Enabled = false;
            browseButton.Enabled = false;
            directoryBox.Enabled = false;
            progressBar.Value = 0;
            detailLabel.Text = String.Empty;
            statusLabel.Text = operation == Operation.Install ? "Installation du patch français…" : "Restauration des fichiers anglais…";
            worker.RunWorkerAsync(new object[] { operation, directory });
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] args = (object[])e.Argument;
            Operation operation = (Operation)args[0];
            string directory = (string)args[1];
            BackgroundWorker currentWorker = (BackgroundWorker)sender;
            Action<string, int> report = delegate(string message, int percent)
            {
                int safePercent = Math.Max(0, Math.Min(100, percent));
                currentWorker.ReportProgress(safePercent, message ?? String.Empty);
            };

            if (operation == Operation.Install)
                InstallerEngine.Install(directory, report);
            else
                InstallerEngine.Restore(directory, report);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, e.ProgressPercentage));
            detailLabel.Text = e.UserState == null ? String.Empty : e.UserState.ToString();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            installButton.Enabled = true;
            restoreButton.Enabled = true;
            browseButton.Enabled = true;
            directoryBox.Enabled = true;

            if (e.Error != null)
            {
                statusLabel.Text = "L’opération n’a pas abouti.";
                detailLabel.Text = "Vérifiez le dossier sélectionné, puis réessayez.";
                MessageBox.Show(this, "Une erreur a empêché l’opération :\r\n\r\n" + e.Error.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            progressBar.Value = 100;
            statusLabel.Text = "Opération terminée.";
            detailLabel.Text = "Vous pouvez maintenant relancer le jeu.";
            MessageBox.Show(this, "L’opération est terminée. Relancez Creeper World 4 pour appliquer le changement.",
                "Terminé", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

