namespace TinyFFR.Tests.Integrations.WinForms {
	partial class MainForm {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			splitContainer = new SplitContainer();
			controlsPanel = new FlowLayoutPanel();
			renderingHeaderLabel = new Label();
			startStopButton = new Button();
			animateCheckBox = new CheckBox();
			renderOnceButton = new Button();
			compositeModeCheckBox = new CheckBox();
			overlayEnabledCheckBox = new CheckBox();
			modelHeaderLabel = new Label();
			loadProgressLabel = new Label();
			loadProgressBar = new ProgressBar();
			modelListBox = new ListBox();
			transformHeaderLabel = new Label();
			spinXCheckBox = new CheckBox();
			spinYCheckBox = new CheckBox();
			spinZCheckBox = new CheckBox();
			shadingHeaderLabel = new Label();
			shadingStyleComboBox = new ComboBox();
			randomColorOpaqueCheckBox = new CheckBox();
			randomizeColorButton = new Button();
			lightingHeaderLabel = new Label();
			shiftLightHueButton = new Button();
			qualityHeaderLabel = new Label();
			qualityComboBox = new ComboBox();
			sceneView = new Egodystonic.TinyFFR.WinForms.TinyFfrSceneView();
			statusLabel = new Label();
			((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
			splitContainer.Panel1.SuspendLayout();
			splitContainer.Panel2.SuspendLayout();
			splitContainer.SuspendLayout();
			controlsPanel.SuspendLayout();
			SuspendLayout();
			// 
			// splitContainer
			// 
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.FixedPanel = FixedPanel.Panel1;
			splitContainer.Location = new Point(0, 0);
			splitContainer.Name = "splitContainer";
			splitContainer.Panel1.Controls.Add(controlsPanel);
			splitContainer.Panel2.Controls.Add(sceneView);
			splitContainer.Panel2.Controls.Add(statusLabel);
			splitContainer.Size = new Size(1400, 800);
			splitContainer.SplitterDistance = 340;
			splitContainer.SplitterWidth = 4;
			splitContainer.TabIndex = 0;
			// 
			// controlsPanel
			// 
			controlsPanel.AutoScroll = true;
			controlsPanel.Controls.Add(renderingHeaderLabel);
			controlsPanel.Controls.Add(startStopButton);
			controlsPanel.Controls.Add(animateCheckBox);
			controlsPanel.Controls.Add(renderOnceButton);
			controlsPanel.Controls.Add(compositeModeCheckBox);
			controlsPanel.Controls.Add(overlayEnabledCheckBox);
			controlsPanel.Controls.Add(modelHeaderLabel);
			controlsPanel.Controls.Add(loadProgressLabel);
			controlsPanel.Controls.Add(loadProgressBar);
			controlsPanel.Controls.Add(modelListBox);
			controlsPanel.Controls.Add(transformHeaderLabel);
			controlsPanel.Controls.Add(spinXCheckBox);
			controlsPanel.Controls.Add(spinYCheckBox);
			controlsPanel.Controls.Add(spinZCheckBox);
			controlsPanel.Controls.Add(shadingHeaderLabel);
			controlsPanel.Controls.Add(shadingStyleComboBox);
			controlsPanel.Controls.Add(randomColorOpaqueCheckBox);
			controlsPanel.Controls.Add(randomizeColorButton);
			controlsPanel.Controls.Add(lightingHeaderLabel);
			controlsPanel.Controls.Add(shiftLightHueButton);
			controlsPanel.Controls.Add(qualityHeaderLabel);
			controlsPanel.Controls.Add(qualityComboBox);
			controlsPanel.Dock = DockStyle.Fill;
			controlsPanel.FlowDirection = FlowDirection.TopDown;
			controlsPanel.Location = new Point(0, 0);
			controlsPanel.Name = "controlsPanel";
			controlsPanel.Padding = new Padding(10);
			controlsPanel.Size = new Size(340, 800);
			controlsPanel.TabIndex = 0;
			controlsPanel.WrapContents = false;
			// 
			// renderingHeaderLabel
			// 
			renderingHeaderLabel.AutoSize = true;
			renderingHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			renderingHeaderLabel.Margin = new Padding(3, 3, 3, 2);
			renderingHeaderLabel.Name = "renderingHeaderLabel";
			renderingHeaderLabel.Text = "Rendering";
			// 
			// startStopButton
			// 
			startStopButton.Name = "startStopButton";
			startStopButton.Size = new Size(300, 28);
			startStopButton.TabIndex = 0;
			startStopButton.Text = "Start / stop rendering";
			startStopButton.UseVisualStyleBackColor = true;
			startStopButton.Click += HandleStartStopClicked;
			// 
			// animateCheckBox
			// 
			animateCheckBox.AutoSize = true;
			animateCheckBox.Checked = true;
			animateCheckBox.CheckState = CheckState.Checked;
			animateCheckBox.Name = "animateCheckBox";
			animateCheckBox.TabIndex = 1;
			animateCheckBox.Text = "Animate (render every tick)";
			animateCheckBox.UseVisualStyleBackColor = true;
			// 
			// renderOnceButton
			// 
			renderOnceButton.Name = "renderOnceButton";
			renderOnceButton.Size = new Size(300, 28);
			renderOnceButton.TabIndex = 2;
			renderOnceButton.Text = "Render once";
			renderOnceButton.UseVisualStyleBackColor = true;
			renderOnceButton.Click += HandleRenderOnceClicked;
			// 
			// compositeModeCheckBox
			// 
			compositeModeCheckBox.AutoSize = true;
			compositeModeCheckBox.Name = "compositeModeCheckBox";
			compositeModeCheckBox.TabIndex = 3;
			compositeModeCheckBox.Text = "Composite mode";
			compositeModeCheckBox.UseVisualStyleBackColor = true;
			compositeModeCheckBox.CheckedChanged += HandleCompositeModeChanged;
			// 
			// overlayEnabledCheckBox
			// 
			overlayEnabledCheckBox.AutoSize = true;
			overlayEnabledCheckBox.Checked = true;
			overlayEnabledCheckBox.CheckState = CheckState.Checked;
			overlayEnabledCheckBox.Enabled = false;
			overlayEnabledCheckBox.Name = "overlayEnabledCheckBox";
			overlayEnabledCheckBox.TabIndex = 4;
			overlayEnabledCheckBox.Text = "Overlay enabled";
			overlayEnabledCheckBox.UseVisualStyleBackColor = true;
			overlayEnabledCheckBox.CheckedChanged += HandleOverlayEnabledChanged;
			// 
			// modelHeaderLabel
			// 
			modelHeaderLabel.AutoSize = true;
			modelHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			modelHeaderLabel.Margin = new Padding(3, 12, 3, 2);
			modelHeaderLabel.Name = "modelHeaderLabel";
			modelHeaderLabel.Text = "Model";
			// 
			// loadProgressLabel
			// 
			loadProgressLabel.AutoSize = false;
			loadProgressLabel.Name = "loadProgressLabel";
			loadProgressLabel.Size = new Size(300, 18);
			loadProgressLabel.Text = "";
			// 
			// loadProgressBar
			// 
			loadProgressBar.Name = "loadProgressBar";
			loadProgressBar.Size = new Size(300, 10);
			loadProgressBar.Style = ProgressBarStyle.Continuous;
			loadProgressBar.TabIndex = 5;
			// 
			// modelListBox
			// 
			modelListBox.FormattingEnabled = true;
			modelListBox.IntegralHeight = false;
			modelListBox.Name = "modelListBox";
			modelListBox.Size = new Size(300, 320);
			modelListBox.TabIndex = 6;
			modelListBox.SelectedIndexChanged += HandleModelSelectionChanged;
			// 
			// transformHeaderLabel
			// 
			transformHeaderLabel.AutoSize = true;
			transformHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			transformHeaderLabel.Margin = new Padding(3, 12, 3, 2);
			transformHeaderLabel.Name = "transformHeaderLabel";
			transformHeaderLabel.Text = "Transform";
			// 
			// spinXCheckBox
			// 
			spinXCheckBox.AutoSize = true;
			spinXCheckBox.Name = "spinXCheckBox";
			spinXCheckBox.TabIndex = 7;
			spinXCheckBox.Text = "Spin X";
			spinXCheckBox.UseVisualStyleBackColor = true;
			spinXCheckBox.CheckedChanged += HandleSpinChanged;
			// 
			// spinYCheckBox
			// 
			spinYCheckBox.AutoSize = true;
			spinYCheckBox.Name = "spinYCheckBox";
			spinYCheckBox.TabIndex = 8;
			spinYCheckBox.Text = "Spin Y";
			spinYCheckBox.UseVisualStyleBackColor = true;
			spinYCheckBox.CheckedChanged += HandleSpinChanged;
			// 
			// spinZCheckBox
			// 
			spinZCheckBox.AutoSize = true;
			spinZCheckBox.Name = "spinZCheckBox";
			spinZCheckBox.TabIndex = 9;
			spinZCheckBox.Text = "Spin Z";
			spinZCheckBox.UseVisualStyleBackColor = true;
			spinZCheckBox.CheckedChanged += HandleSpinChanged;
			// 
			// shadingHeaderLabel
			// 
			shadingHeaderLabel.AutoSize = true;
			shadingHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			shadingHeaderLabel.Margin = new Padding(3, 12, 3, 2);
			shadingHeaderLabel.Name = "shadingHeaderLabel";
			shadingHeaderLabel.Text = "Shading";
			// 
			// shadingStyleComboBox
			// 
			shadingStyleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			shadingStyleComboBox.FormattingEnabled = true;
			shadingStyleComboBox.Name = "shadingStyleComboBox";
			shadingStyleComboBox.Size = new Size(300, 23);
			shadingStyleComboBox.TabIndex = 10;
			shadingStyleComboBox.SelectedIndexChanged += HandleShadingStyleChanged;
			// 
			// randomColorOpaqueCheckBox
			// 
			randomColorOpaqueCheckBox.AutoSize = true;
			randomColorOpaqueCheckBox.Checked = true;
			randomColorOpaqueCheckBox.CheckState = CheckState.Checked;
			randomColorOpaqueCheckBox.Name = "randomColorOpaqueCheckBox";
			randomColorOpaqueCheckBox.TabIndex = 11;
			randomColorOpaqueCheckBox.Text = "Opaque random colors";
			randomColorOpaqueCheckBox.UseVisualStyleBackColor = true;
			// 
			// randomizeColorButton
			// 
			randomizeColorButton.Name = "randomizeColorButton";
			randomizeColorButton.Size = new Size(300, 28);
			randomizeColorButton.TabIndex = 12;
			randomizeColorButton.Text = "Randomize color";
			randomizeColorButton.UseVisualStyleBackColor = true;
			randomizeColorButton.Click += HandleRandomizeColorClicked;
			// 
			// lightingHeaderLabel
			// 
			lightingHeaderLabel.AutoSize = true;
			lightingHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			lightingHeaderLabel.Margin = new Padding(3, 12, 3, 2);
			lightingHeaderLabel.Name = "lightingHeaderLabel";
			lightingHeaderLabel.Text = "Lighting";
			// 
			// shiftLightHueButton
			// 
			shiftLightHueButton.Name = "shiftLightHueButton";
			shiftLightHueButton.Size = new Size(300, 28);
			shiftLightHueButton.TabIndex = 13;
			shiftLightHueButton.Text = "Shift light hue";
			shiftLightHueButton.UseVisualStyleBackColor = true;
			shiftLightHueButton.Click += HandleShiftLightHueClicked;
			// 
			// qualityHeaderLabel
			// 
			qualityHeaderLabel.AutoSize = true;
			qualityHeaderLabel.Font = new Font(Font, FontStyle.Bold);
			qualityHeaderLabel.Margin = new Padding(3, 12, 3, 2);
			qualityHeaderLabel.Name = "qualityHeaderLabel";
			qualityHeaderLabel.Text = "Quality";
			// 
			// qualityComboBox
			// 
			qualityComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			qualityComboBox.FormattingEnabled = true;
			qualityComboBox.Name = "qualityComboBox";
			qualityComboBox.Size = new Size(300, 23);
			qualityComboBox.TabIndex = 14;
			qualityComboBox.SelectedIndexChanged += HandleQualityChanged;
			// 
			// sceneView
			// 
			sceneView.Dock = DockStyle.Fill;
			sceneView.Location = new Point(0, 0);
			sceneView.Name = "sceneView";
			sceneView.Size = new Size(1056, 774);
			sceneView.TabIndex = 0;
			// 
			// statusLabel
			// 
			statusLabel.AutoEllipsis = true;
			statusLabel.Dock = DockStyle.Bottom;
			statusLabel.Location = new Point(0, 774);
			statusLabel.Name = "statusLabel";
			statusLabel.Padding = new Padding(8, 4, 8, 4);
			statusLabel.Size = new Size(1056, 26);
			statusLabel.Text = "Not rendering.";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1400, 800);
			Controls.Add(splitContainer);
			Name = "MainForm";
			Text = "TinyFFR WinForms Integration - Model Viewer";
			splitContainer.Panel1.ResumeLayout(false);
			splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
			splitContainer.ResumeLayout(false);
			controlsPanel.ResumeLayout(false);
			controlsPanel.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private SplitContainer splitContainer;
		private FlowLayoutPanel controlsPanel;
		private Label renderingHeaderLabel;
		private Button startStopButton;
		private CheckBox animateCheckBox;
		private Button renderOnceButton;
		private CheckBox compositeModeCheckBox;
		private CheckBox overlayEnabledCheckBox;
		private Label modelHeaderLabel;
		private Label loadProgressLabel;
		private ProgressBar loadProgressBar;
		private ListBox modelListBox;
		private Label transformHeaderLabel;
		private CheckBox spinXCheckBox;
		private CheckBox spinYCheckBox;
		private CheckBox spinZCheckBox;
		private Label shadingHeaderLabel;
		private ComboBox shadingStyleComboBox;
		private CheckBox randomColorOpaqueCheckBox;
		private Button randomizeColorButton;
		private Label lightingHeaderLabel;
		private Button shiftLightHueButton;
		private Label qualityHeaderLabel;
		private ComboBox qualityComboBox;
		private Egodystonic.TinyFFR.WinForms.TinyFfrSceneView sceneView;
		private Label statusLabel;
	}
}
