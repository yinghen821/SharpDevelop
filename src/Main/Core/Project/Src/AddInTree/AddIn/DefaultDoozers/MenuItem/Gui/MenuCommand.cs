﻿// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ICSharpCode.Core
{
	public class MenuCommand : ToolStripMenuItem, IStatusUpdate
	{
		object caller;
		Codon codon;
		ICommand menuCommand = null;
		string description = "";
		string localizedText = null;
		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		public ICommand Command {
			get {
				if (menuCommand == null) {
					CreateCommand();
				}
				return menuCommand;
			}
		}
		
		// HACK: find a better way to allow the host app to process link commands
		public static Converter<string, ICommand> LinkCommandCreator;
		
		void CreateCommand()
		{
			try {
				string link = codon.Properties["link"];
				if (link != null && link.Length > 0) {
					if (LinkCommandCreator == null)
						throw new NotSupportedException("MenuCommand.LinkCommandCreator is not set, cannot create LinkCommands.");
					menuCommand = LinkCommandCreator(codon.Properties["link"]);
				} else {
					menuCommand = (ICommand)codon.AddIn.CreateObject(codon.Properties["class"]);
				}
				menuCommand.Owner = caller;
			} catch (Exception e) {
				MessageService.ShowError(e, "Can't create menu command : " + codon.Id);
			}
		}
		
		public MenuCommand(Codon codon, object caller) : this(codon, caller, false)
		{
			
		}
		
		public static Keys ParseShortcut(string shortcutString)
		{
			Keys shortCut = Keys.None;
			try {
				foreach (string key in shortcutString.Split('|')) {
					shortCut  |= (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), key);
				}
			} catch (Exception) {
				return System.Windows.Forms.Keys.None;
			}
			return shortCut;
		}
		
		public MenuCommand(Codon codon, object caller, bool createCommand)
		{
			this.RightToLeft = RightToLeft.Inherit;
			this.caller        = caller;
			this.codon         = codon;
			
			if (createCommand) {
				CreateCommand();
			}
			
			if (codon.Properties.Contains("shortcut")) {
				ShortcutKeys =  ParseShortcut(codon.Properties["shortcut"]);
			}
		}
		
		public MenuCommand(string label, EventHandler handler) : this(label)
		{
			this.Click  += handler;
		}
		
		public MenuCommand(string label)
		{
			this.RightToLeft = RightToLeft.Inherit;
			this.codon  = null;
			this.caller = null;
			Text = StringParser.Parse(label);
		}
		
		protected override void OnClick(System.EventArgs e)
		{
			base.OnClick(e);
			if (codon != null) {
				if (GetVisible() && Enabled) {
					ICommand cmd = Command;
					if (cmd != null) cmd.Run();
				}
			}
		}
		
//		protected override void OnSelect(System.EventArgs e)
//		{
//			base.OnSelect(e);
//			StatusBarService.SetMessage(description);
//		}
		
		
		public override bool Enabled {
			get {
				if (codon == null) {
					return base.Enabled;
				}
				ConditionFailedAction failedAction = codon.GetFailedAction(caller);
				bool isEnabled = failedAction != ConditionFailedAction.Disable;
				
				if (menuCommand != null && menuCommand is IMenuCommand) {
					isEnabled &= ((IMenuCommand)menuCommand).IsEnabled;
				}
				return isEnabled;
			}
		}
		
		bool GetVisible()
		{
			if (codon == null)
				return true;
			else
				return codon.GetFailedAction(caller) != ConditionFailedAction.Exclude;
		}
		
		public virtual void UpdateStatus()
		{
			if (codon != null) {
				if (Image == null && codon.Properties.Contains("icon")) {
					Image = ResourceService.GetBitmap(codon.Properties["icon"]);
				}
				Visible = GetVisible();
				
				if (localizedText == null) {
					localizedText = codon.Properties["label"];
				}
			}
			if (localizedText != null) {
				Text = StringParser.Parse(localizedText);
			}
		}
	}
}
