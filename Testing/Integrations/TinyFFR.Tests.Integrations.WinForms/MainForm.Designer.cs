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
			toggleRenderingButton = new Button();
			changeLightColourButton = new Button();
			compositeModeCheckBox = new CheckBox();
			toggleOverlayButton = new Button();
			sceneView = new Egodystonic.TinyFFR.WinForms.TinyFfrSceneView();
			SuspendLayout();
			// 
			// toggleRenderingButton
			// 
			toggleRenderingButton.Location = new Point(12, 12);
			toggleRenderingButton.Name = "toggleRenderingButton";
			toggleRenderingButton.Size = new Size(203, 34);
			toggleRenderingButton.TabIndex = 0;
			toggleRenderingButton.Text = "Toggle Rendering";
			toggleRenderingButton.UseVisualStyleBackColor = true;
			toggleRenderingButton.Click += toggleRenderingButton_Click;
			// 
			// changeLightColourButton
			// 
			changeLightColourButton.Location = new Point(221, 12);
			changeLightColourButton.Name = "changeLightColourButton";
			changeLightColourButton.Size = new Size(254, 34);
			changeLightColourButton.TabIndex = 1;
			changeLightColourButton.Text = "Change Light Colour";
			changeLightColourButton.UseVisualStyleBackColor = true;
			changeLightColourButton.Click += changeLightColourButton_Click;
			//
			// compositeModeCheckBox
			//
			compositeModeCheckBox.Location = new Point(481, 12);
			compositeModeCheckBox.Name = "compositeModeCheckBox";
			compositeModeCheckBox.Size = new Size(203, 34);
			compositeModeCheckBox.TabIndex = 3;
			compositeModeCheckBox.Text = "Composite Mode";
			compositeModeCheckBox.UseVisualStyleBackColor = true;
			//
			// toggleOverlayButton
			//
			toggleOverlayButton.Location = new Point(690, 12);
			toggleOverlayButton.Name = "toggleOverlayButton";
			toggleOverlayButton.Size = new Size(203, 34);
			toggleOverlayButton.TabIndex = 4;
			toggleOverlayButton.Text = "Toggle Overlay";
			toggleOverlayButton.UseVisualStyleBackColor = true;
			toggleOverlayButton.Click += toggleOverlayButton_Click;
			//
			// sceneView
			//
			sceneView.InternalRenderResolution = new Size(300, 150);
			sceneView.Location = new Point(12, 64);
			sceneView.Name = "sceneView";
			sceneView.Size = new Size(1933, 1146);
			sceneView.TabIndex = 2;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(10F, 25F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1957, 1222);
			Controls.Add(sceneView);
			Controls.Add(changeLightColourButton);
			Controls.Add(toggleRenderingButton);
			Controls.Add(compositeModeCheckBox);
			Controls.Add(toggleOverlayButton);
			Name = "MainForm";
			Text = "MainForm";
			ResumeLayout(false);
		}

		#endregion

		private Button toggleRenderingButton;
		private Button changeLightColourButton;
		private CheckBox compositeModeCheckBox;
		private Button toggleOverlayButton;
		private Egodystonic.TinyFFR.WinForms.TinyFfrSceneView sceneView;
	}
}
