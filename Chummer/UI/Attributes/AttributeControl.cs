﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chummer.Backend.Attributes;
using Chummer.helpers;

namespace Chummer.UI.Attributes
{
    public partial class AttributeControl : UserControl
	{
		// ConnectionRatingChanged Event Handler.
		public delegate void ValueChangedHandler(Object sender);
		public event ValueChangedHandler ValueChanged;
		private readonly CharacterAttrib attribute;
		private object sender;
		private decimal _oldBase;
		private decimal _oldKarma;

		public AttributeControl(CharacterAttrib attribute)
        {
            this.attribute = attribute;
            InitializeComponent();
			
			//Display
			lblName.DataBindings.Add("Text", attribute, nameof(CharacterAttrib.DisplayNameFormatted), false, DataSourceUpdateMode.OnPropertyChanged);
            lblValue.DataBindings.Add("Text", attribute, nameof(CharacterAttrib.DisplayValue), false, DataSourceUpdateMode.OnPropertyChanged);
            lblLimits.DataBindings.Add("Text", attribute, nameof(CharacterAttrib.AugmentedMetatypeLimits), false, DataSourceUpdateMode.OnPropertyChanged);
            lblValue.DataBindings.Add("TooltipText", attribute, nameof(CharacterAttrib.ToolTip), false, DataSourceUpdateMode.OnPropertyChanged);
            if (attribute._objCharacter.Created)
            {
                nudBase.Visible = false;
                nudKarma.Visible = false;
                cmdImproveATT.Visible = true;
	            cmdBurnEdge.Visible = attribute.Abbrev == "EDG";
            }
            else
			{
				nudBase.DataBindings.Add("Value", attribute, nameof(CharacterAttrib.Base), false, DataSourceUpdateMode.OnPropertyChanged);
				nudBase.DataBindings.Add("Minimum", attribute, nameof(CharacterAttrib.TotalMinimum), false, DataSourceUpdateMode.OnPropertyChanged);
                nudBase.DataBindings.Add("Maximum", attribute, nameof(CharacterAttrib.PriorityMaximum), false, DataSourceUpdateMode.OnPropertyChanged);
                nudBase.DataBindings.Add("Enabled", attribute, nameof(CharacterAttrib.BaseUnlocked), false, DataSourceUpdateMode.OnPropertyChanged);
				nudBase.DataBindings.Add("InterceptMouseWheel", attribute._objCharacter.Options, nameof(CharacterOptions.InterceptMode), false,
					DataSourceUpdateMode.OnPropertyChanged);
				nudBase.Visible = true;

				nudKarma.Minimum = 0;
				nudKarma.DataBindings.Add("Maximum", attribute, nameof(CharacterAttrib.KarmaMaximum), false, DataSourceUpdateMode.OnPropertyChanged);
                nudKarma.DataBindings.Add("Value", attribute, nameof(CharacterAttrib.Karma), false, DataSourceUpdateMode.OnPropertyChanged);
				nudKarma.DataBindings.Add("InterceptMouseWheel", attribute._objCharacter.Options, nameof(CharacterOptions.InterceptMode), false,
					DataSourceUpdateMode.OnPropertyChanged);
				nudKarma.Visible = true;
                cmdImproveATT.Visible = false;
	            cmdBurnEdge.Visible = false;
				
            }
        }

		public AttributeControl(object sender)
		{
			this.sender = sender;
		}

		private void cmdImproveATT_Click(object sender, EventArgs e)
        {
            frmCareer parent = ParentForm as frmCareer;
            if (parent != null)
            {
                int upgradeKarmaCost = attribute.UpgradeKarmaCost();

                if (upgradeKarmaCost == -1) return; //TODO: more descriptive
				string confirmstring = string.Format(LanguageManager.Instance.GetString("Message_ConfirmKarmaExpense"),
					attribute.DisplayNameFormatted, attribute.Value + 1, upgradeKarmaCost);
				if (upgradeKarmaCost > attribute._objCharacter.Karma)
                {
                    MessageBox.Show(LanguageManager.Instance.GetString("Message_NotEnoughKarma"), LanguageManager.Instance.GetString("MessageTitle_NotEnoughKarma"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!parent.ConfirmKarmaExpense(confirmstring))
                    return;
            }
            attribute.Upgrade();
	        ValueChanged?.Invoke(this);
        }

		private void nudBase_ValueChanged(object sender, EventArgs e)
		{
			decimal d = ((NumericUpDownEx) sender).Value;
			if (!ShowAttributeRule(d))
			{
				nudBase.Value = _oldBase;
				return;
			}
			ValueChanged?.Invoke(this);
			_oldBase = d;
		}

		private void nudKarma_ValueChanged(object sender, EventArgs e)
		{
			decimal d = ((NumericUpDownEx)sender).Value;
			if (!ShowAttributeRule(d))
			{
				nudKarma.Value = _oldKarma;
				return;
			}
			ValueChanged?.Invoke(this);
			_oldKarma = d;
		}

		/// <summary>
		/// Show the dialogue that notifies the user that characters cannot have more than 1 Attribute at its maximum value during character creation.
		/// </summary>
		private bool ShowAttributeRule(decimal value)
		{
			if (value < attribute.TotalMaximum) return true;
			if (!attribute._objCharacter.AttributeList.Any(att => att.Value.AtMetatypeMaximum && att.Value.Abbrev != attribute.Abbrev)) return true;
		    if (!attribute.AtMetatypeMaximum)
		    {
		        MessageBox.Show(LanguageManager.Instance.GetString("Message_AttributeMaximum"),
		            LanguageManager.Instance.GetString("MessageTitle_Attribute"), MessageBoxButtons.OK,
		            MessageBoxIcon.Information);
		    }
		    return false;
		}

        public string AttributeName
	    {
		    get { return attribute.Abbrev; }
	    }

		private void cmdBurnEdge_Click(object sender, EventArgs e)
		{
			// Edge cannot go below 1.
			if (attribute.Value == 0)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotBurnEdge"), LanguageManager.Instance.GetString("MessageTitle_CannotBurnEdge"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// Verify that the user wants to Burn a point of Edge.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_BurnEdge"), LanguageManager.Instance.GetString("MessageTitle_BurnEdge"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			attribute.Degrade(1);
			ValueChanged?.Invoke(this);
		}

        private void nudBase_BeforeValueIncrement(object sender, CancelEventArgs e)
        {
            if (nudBase.Value + nudKarma.Value == attribute.TotalMaximum && nudKarma.Value != nudKarma.Minimum)
            {
                nudKarma.Value -= nudBase.Increment;
            }
        }

        private void nudKarma_BeforeValueIncrement(object sender, CancelEventArgs e)
        {
            if (nudBase.Value + nudKarma.Value == attribute.TotalMaximum && nudBase.Value != nudBase.Minimum)
            {
                nudBase.Value -= nudKarma.Increment;
            }
        }
    }
}