using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer.Backend.Equipment
{
    /// <summary>
    /// Standard Character Gear.
    /// </summary>
    public class Gear : INamedParentWithGuidAndNode<Gear>
    {
        protected Guid _guiID;
        protected string _SourceGuid;
        protected string _strName = string.Empty;
        protected string _strCategory = string.Empty;
        protected int _intMaxRating = 0;
        protected int _intMinRating = 0;
        protected int _intRating = 0;
        protected int _intQty = 1;
        protected string _strCapacity = string.Empty;
        protected string _strArmorCapacity = string.Empty;
        protected string _strAvail = string.Empty;
        protected int _intCostFor = 1;
        protected string _strDeviceRating = string.Empty;
        protected string _strCost = string.Empty;
        protected string _strSource = string.Empty;
        protected string _strPage = string.Empty;
        protected string _strExtra = string.Empty;
        protected bool _blnBonded = false;
        protected bool _blnEquipped = true;
        protected bool _blnWirelessOn = true;
        protected bool _blnHomeNode = false;
        protected XmlNode _nodBonus;
        protected XmlNode _nodWirelessBonus;
        protected XmlNode _nodWeaponBonus;
        protected Guid _guiWeaponID = new Guid();
        protected List<Gear> _objChildren = new List<Gear>();
        protected string _strNotes = string.Empty;
        protected string _strLocation = string.Empty;
        protected Character _objCharacter;
        protected string _strAltName = string.Empty;
        protected string _strAltCategory = string.Empty;
        protected string _strAltPage = string.Empty;
        private int _intChildCostMultiplier = 1;
        private int _intChildAvailModifier = 0;
        protected Gear _objParent = null;
        protected bool _blnDiscountCost = false;
        protected string _strGearName = string.Empty;
        protected bool _blnIncludedInParent = false;
        protected int _intMatrixCMBonus = 0;
        protected int _intMatrixCMFilled = 0;

        #region Constructor, Create, Save, Load, and Print Methods
        public Gear(Character objCharacter)
        {
            // Create the GUID for the new piece of Gear.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// Create a Gear from an XmlNode and return the TreeNodes for it.
        /// <param name="objXmlGear">XmlNode to create the object from.</param>
        /// <param name="objCharacter">Character the Gear is being added to.</param>
        /// <param name="objNode">TreeNode to populate a TreeView.</param>
        /// <param name="intRating">Selected Rating for the Gear.</param>
        /// <param name="objWeapons">List of Weapons that should be added to the character.</param>
        /// <param name="objWeaponNodes">List of TreeNodes to represent the added Weapons</param>
        /// <param name="strForceValue">Value to forcefully select for any ImprovementManager prompts.</param>
        /// <param name="blnHacked">Whether or not a Matrix Program has been hacked (removing the Copy Protection and Registration plugins).</param>
        /// <param name="blnInherent">Whether or not a Program is Inherent to an A.I.</param>
        /// <param name="blnAddImprovements">Whether or not Improvements should be added to the character.</param>
        /// <param name="blnCreateChildren">Whether or not child Gear should be created.</param>
        /// <param name="blnAerodynamic">Whether or not Weapons should be created as Aerodynamic.</param>
        public void Create(XmlNode objXmlGear, Character objCharacter, TreeNode objNode, int intRating, List<Weapon> objWeapons, List<TreeNode> objWeaponNodes, string strForceValue = "", bool blnHacked = false, bool blnInherent = false, bool blnAddImprovements = true, bool blnCreateChildren = true, bool blnAerodynamic = false)
        {
            if (objXmlGear == null)
                return;
            XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
            objXmlGear.TryGetStringFieldQuickly("id", ref _SourceGuid);
            objXmlGear.TryGetStringFieldQuickly("name", ref _strName);
            objXmlGear.TryGetStringFieldQuickly("category", ref _strCategory);
            objXmlGear.TryGetStringFieldQuickly("avail", ref _strAvail);
            objXmlGear.TryGetStringFieldQuickly("capacity", ref _strCapacity);
            objXmlGear.TryGetStringFieldQuickly("armorcapacity", ref _strArmorCapacity);
            objXmlGear.TryGetInt32FieldQuickly("costfor", ref _intCostFor);
            _intQty = _intCostFor;
            objXmlGear.TryGetStringFieldQuickly("cost", ref _strCost);
            _nodBonus = objXmlGear["bonus"];
            _nodWirelessBonus = objXmlGear["wirelessbonus"];
            _blnWirelessOn = _nodWirelessBonus != null;
            objXmlGear.TryGetInt32FieldQuickly("rating", ref _intMaxRating);
            objXmlGear.TryGetInt32FieldQuickly("minrating", ref _intMinRating);
            objXmlGear.TryGetStringFieldQuickly("devicerating", ref _strDeviceRating);
            objXmlGear.TryGetInt32FieldQuickly("matrixcmbonus", ref _intMatrixCMBonus);
            _intRating = intRating;
            objXmlGear.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlGear.TryGetStringFieldQuickly("page", ref _strPage);
            objXmlGear.TryGetInt32FieldQuickly("childcostmultiplier", ref _intChildCostMultiplier);
            objXmlGear.TryGetInt32FieldQuickly("childavailmodifier", ref _intChildAvailModifier);

            if (GlobalOptions.Instance.Language != "en-us")
            {
                XmlNode objGearNode = MyXmlNode;
                if (objGearNode != null)
                {
                    objGearNode.TryGetStringFieldQuickly("translate", ref _strAltName);
                    objGearNode.TryGetStringFieldQuickly("altpage", ref _strAltPage);
                }

                if (_strAltName.StartsWith("Stacked Focus"))
                    _strAltName = _strAltName.Replace("Stacked Focus", LanguageManager.Instance.GetString("String_StackedFocus"));

                objGearNode = objXmlDocument.SelectSingleNode("/chummer/categories/category[. = \"" + _strCategory + "\"]");
                _strAltCategory = objGearNode?.Attributes?["translate"]?.InnerText;
            }

            // Check for a Custom name
            if (_strName == "Custom Item")
            {
                if (string.IsNullOrEmpty(strForceValue))
                {
                    frmSelectText frmPickText = new frmSelectText();
                    frmPickText.PreventXPathErrors = true;
                    frmPickText.Description = LanguageManager.Instance.GetString("String_CustomItem_SelectText");
                    frmPickText.ShowDialog();

                    // Make sure the dialogue window was not canceled.
                    if (frmPickText.DialogResult != DialogResult.Cancel)
                    {
                        _strName = frmPickText.SelectedValue;
                    }
                }
                else
                {
                    string strCustomName = LanguageManager.Instance.GetString(strForceValue, false);
                    if (string.IsNullOrEmpty(strCustomName))
                        strCustomName = LanguageManager.Instance.TranslateExtra(strForceValue);
                    _strName = strCustomName;
                }
            }
            // Check for a Variable Cost.
            if (!string.IsNullOrEmpty(_strCost))
            {
                if (_strCost.StartsWith("Variable") && string.IsNullOrEmpty(strForceValue))
                {
                    int intMin;
                    int intMax = 0;
                    string strCost = _strCost.Replace("Variable(", string.Empty).Replace(")", string.Empty);
                    if (strCost.Contains("-"))
                    {
                        string[] strValues = strCost.Split('-');
                        intMin = Convert.ToInt32(strValues[0]);
                        intMax = Convert.ToInt32(strValues[1]);
                    }
                    else
                        intMin = Convert.ToInt32(strCost.Replace("+", string.Empty));

                    if (intMin != 0 || intMax != 0)
                    {
                        frmSelectNumber frmPickNumber = new frmSelectNumber();
                        if (intMax == 0)
                            intMax = 1000000;
                        frmPickNumber.Minimum = intMin;
                        frmPickNumber.Maximum = intMax;
                        frmPickNumber.Description = LanguageManager.Instance.GetString("String_SelectVariableCost").Replace("{0}", DisplayNameShort);
                        frmPickNumber.AllowCancel = false;
                        frmPickNumber.ShowDialog();
                        _strCost = frmPickNumber.SelectedValue.ToString();
                    }
                }
            }

            string strSource = _guiID.ToString();

            objNode.Text = _strName;
            objNode.Tag = _guiID.ToString();

            // If the Gear is Ammunition, ask the user to select a Weapon Category for it to be limited to.
            if (_strCategory == "Ammunition" && (_strName.StartsWith("Ammo:") || _strName.StartsWith("Arrow:") || _strName.StartsWith("Bolt:")))
            {
                frmSelectWeaponCategory frmPickWeaponCategory = new frmSelectWeaponCategory();
                frmPickWeaponCategory.Description = LanguageManager.Instance.GetString("String_SelectWeaponCategoryAmmo");
                if (!string.IsNullOrEmpty(strForceValue) && !strForceValue.Equals(_strName))
                    frmPickWeaponCategory.OnlyCategory = strForceValue;

                //should really go in a data file
                if (_strName.StartsWith("Ammo:"))
                {
                    if (_strName.StartsWith("Ammo: Assault Cannon") || _strName.StartsWith("Ammo: Gauss"))
                    {
                        frmPickWeaponCategory.WeaponType = "cannon";
                    }
                    else if (_strName.StartsWith("Ammo: Taser Dart"))
                    {
                        frmPickWeaponCategory.WeaponType = "taser";
                    }
                    else if(_strName.StartsWith("Ammo: Fuel Canister"))
                    {
                        frmPickWeaponCategory.WeaponType = "flame";
                    }
                    else if (_strName.StartsWith("Ammo: Injection Dart"))
                    {
                        frmPickWeaponCategory.WeaponType = "exotic";
                    }
                    else if (_strName.StartsWith("Ammo: Peak-Discharge"))
                    {
                        frmPickWeaponCategory.WeaponType = "laser";
                    }
                    else
                    {
                        frmPickWeaponCategory.WeaponType = "gun";
                    }
                }
                else if (_strName.StartsWith("Arrow:"))
                {
                    frmPickWeaponCategory.WeaponType = "bow";
                }
                else if (_strName.StartsWith("Bolt:"))
                {
                    frmPickWeaponCategory.WeaponType = "crossbow";
                }
                frmPickWeaponCategory.ShowDialog();

                _strExtra = frmPickWeaponCategory.SelectedCategory;
                objNode.Text += " (" + _strExtra + ")";
            }

            // Add Gear Weapons if applicable.
            if (objXmlGear.InnerXml.Contains("<addweapon>"))
            {
                XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");

                // More than one Weapon can be added, so loop through all occurrences.
                foreach (XmlNode objXmlAddWeapon in objXmlGear.SelectNodes("addweapon"))
                {
                    var objXmlWeapon = helpers.Guid.IsGuid(objXmlAddWeapon.InnerText)
                        ? objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[id = \"" + objXmlAddWeapon.InnerText + "\"]")
                        : objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlAddWeapon.InnerText + "\"]");

                    TreeNode objGearWeaponNode = new TreeNode();
                    Weapon objGearWeapon = new Weapon(objCharacter);
                    objGearWeapon.Create(objXmlWeapon, objCharacter, objGearWeaponNode, null, null);
                    objGearWeapon.ParentID = InternalId;
                    objGearWeaponNode.ForeColor = SystemColors.GrayText;
                    if (blnAerodynamic)
                    {
                        objGearWeapon.Name += " (" + LanguageManager.Instance.GetString("Checkbox_Aerodynamic") + ")";
                        objGearWeapon.Range = "Aerodynamic Grenades";
                        objGearWeaponNode.Text = objGearWeapon.DisplayName;
                        _strName += " (" + LanguageManager.Instance.GetString("Checkbox_Aerodynamic") + ")";
                        objNode.Text = DisplayName;
                    }

                    objWeaponNodes.Add(objGearWeaponNode);
                    objWeapons.Add(objGearWeapon);

                    _guiWeaponID = Guid.Parse(objGearWeapon.InternalId);
                }
            }

            // If the item grants a bonus, pass the information to the Improvement Manager.
            if (objXmlGear.InnerXml.Contains("<bonus>"))
            {
                // Do not apply the Improvements if this is a Focus, unless we're speicifically creating a Weapon Focus. This is to avoid creating the Foci's Improvements twice (once when it's first added
                // to the character which is incorrect, and once when the Focus is actually Bonded).
                bool blnApply = !((_strCategory == "Foci" || _strCategory == "Metamagic Foci") && !objXmlGear["bonus"].InnerXml.Contains("selectweapon"));

                if (blnApply)
                {
                    ImprovementManager.ForcedValue = strForceValue;
                    if (!ImprovementManager.CreateImprovements(blnAddImprovements ? objCharacter : null, Improvement.ImprovementSource.Gear, strSource, objXmlGear["bonus"], false, intRating, DisplayNameShort))
                    {
                        _guiID = Guid.Empty;
                        return;
                    }
                    if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                    {
                        _strExtra = ImprovementManager.SelectedValue;
                        objNode.Text += " (" + ImprovementManager.SelectedValue + ")";
                    }
                }
            }

            // Check to see if there are any child elements.
            if (objXmlGear.InnerXml.Contains("<gears>") && blnCreateChildren)
            {
                CreateChildren(objXmlDocument, objXmlGear, this, objNode, objCharacter, blnHacked, blnAddImprovements);
            }

            // Add the Copy Protection and Registration plugins to the Matrix program. This does not apply if Unwired is not enabled, Hacked is selected, or this is a Suite being added (individual programs will add it to themselves).
            if (blnCreateChildren)
            {
                if ((_strCategory == "Matrix Programs" || _strCategory == "Skillsofts" || _strCategory == "Autosofts" || _strCategory == "Autosofts, Agent" || _strCategory == "Autosofts, Drone") && objCharacter.Options.BookEnabled("UN") && !blnHacked && !_strName.StartsWith("Suite:"))
                {
                    if (_objCharacter.Options.AutomaticCopyProtection && !blnInherent)
                    {
                        Gear objPlugin1 = new Gear(_objCharacter);
                        TreeNode objPlugin1Node = new TreeNode();
                        objPlugin1.Create(objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Copy Protection\"]"), objCharacter, objPlugin1Node, _intRating, null, null);
                        if (_intRating == 0)
                            objPlugin1.Rating = 1;
                        objPlugin1.Avail = "0";
                        objPlugin1.Cost = "0";
                        objPlugin1.Capacity = "[0]";
                        objPlugin1.Parent = this;
                        _objChildren.Add(objPlugin1);
                        if ((this as Commlink)?.CanSwapAttributes == true)
                        {
                            (this as Commlink).RefreshCyberdeckArray();
                        }
                        objNode.Nodes.Add(objPlugin1Node);
                    }

                    if (_objCharacter.Options.AutomaticRegistration && !blnInherent)
                    {
                        Gear objPlugin2 = new Gear(_objCharacter);
                        TreeNode objPlugin2Node = new TreeNode();
                        objPlugin2.Create(objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Registration\"]"), objCharacter, objPlugin2Node, 0, null, null);
                        objPlugin2.Avail = "0";
                        objPlugin2.Cost = "0";
                        objPlugin2.Capacity = "[0]";
                        objPlugin2.Parent = this;
                        _objChildren.Add(objPlugin2);
                        if ((this as Commlink)?.CanSwapAttributes == true)
                        {
                            (this as Commlink).RefreshCyberdeckArray();
                        }
                        objNode.Nodes.Add(objPlugin2Node);
                        objNode.Expand();
                    }

                    if ((objCharacter.Metatype == "A.I." || objCharacter.MetatypeCategory == "Technocritters" || objCharacter.MetatypeCategory == "Protosapients") && blnInherent)
                    {
                        Gear objPlugin3 = new Gear(_objCharacter);
                        TreeNode objPlugin3Node = new TreeNode();
                        objPlugin3.Create(objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Ergonomic\"]"), objCharacter, objPlugin3Node, 0, null, null);
                        objPlugin3.Avail = "0";
                        objPlugin3.Cost = "0";
                        objPlugin3.Capacity = "[0]";
                        objPlugin3.Parent = this;
                        _objChildren.Add(objPlugin3);
                        if ((this as Commlink)?.CanSwapAttributes == true)
                        {
                            (this as Commlink).RefreshCyberdeckArray();
                        }
                        objNode.Nodes.Add(objPlugin3Node);

                        Gear objPlugin4 = new Gear(_objCharacter);
                        TreeNode objPlugin4Node = new TreeNode();
                        objPlugin4.Create(objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Optimization\" and category = \"Program Options\"]"), objCharacter, objPlugin4Node, _intRating, null, null);
                        if (_intRating == 0)
                            objPlugin4.Rating = 1;
                        objPlugin4.Avail = "0";
                        objPlugin4.Cost = "0";
                        objPlugin4.Capacity = "[0]";
                        objPlugin4.Parent = this;
                        _objChildren.Add(objPlugin4);
                        if ((this as Commlink)?.CanSwapAttributes == true)
                        {
                            (this as Commlink).RefreshCyberdeckArray();
                        }
                        objNode.Nodes.Add(objPlugin4Node);
                        objNode.Expand();
                    }
                }
            }

            // If the item grants a Weapon bonus (Ammunition), just fill the WeaponBonus XmlNode.
            _nodWeaponBonus = objXmlGear["weaponbonus"];
            objNode.Text = DisplayName;
        }

        public void CreateChildren(XmlDocument objXmlGearDocument, XmlNode objXmlGear, Gear objParent, TreeNode objNode, Character objCharacter, bool blnHacked, bool blnAddImprovements)
        {
            bool blnStartCollapsed = objXmlGear["gears"]?.Attributes?["startcollapsed"]?.InnerText == "yes";
            // Create Gear by looking up the name of the item we're provided with.
            if (objXmlGear.SelectNodes("gears/usegear").Count > 0)
            {
                foreach (XmlNode objXmlChild in objXmlGear.SelectNodes("gears/usegear"))
                {
                    CreateChild(objXmlGearDocument, objXmlChild, objParent, objNode, objCharacter, blnHacked, blnAddImprovements);
                }
            }
            // Create Gear by choosing from pre-determined lists.
            if (objXmlGear.SelectNodes("gears/choosegear").Count > 0)
            {
                bool blnCancelledDialog = false;
                List<XmlNode> lstChildrenToCreate = new List<XmlNode>();
                foreach (XmlNode objXmlChooseGearNode in objXmlGear.SelectNodes("gears/choosegear"))
                {
                    // Each list is processed on its own and has usegear members
                    XmlNodeList objXmlNodeList = objXmlChooseGearNode.SelectNodes("usegear");

                    List<ListItem> lstGears = new List<ListItem>();
                    foreach (XmlNode objChoiceNode in objXmlNodeList)
                    {
                        XmlNode objXmlLoopGear = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objChoiceNode["name"].InnerText + "\" and category = \"" + objChoiceNode["category"].InnerText + "\"]");
                        if (objXmlLoopGear == null)
                            continue;
                        if (objXmlLoopGear["forbidden"]?["geardetails"] != null)
                        {
                            // Assumes topmost parent is an AND node
                            if (objXmlGear.ProcessFilterOperationNode(objXmlLoopGear["forbidden"]["geardetails"], false))
                            {
                                continue;
                            }
                        }
                        if (objXmlLoopGear["required"]?["geardetails"] != null)
                        {
                            // Assumes topmost parent is an AND node
                            if (!objXmlGear.ProcessFilterOperationNode(objXmlLoopGear["required"]["geardetails"], false))
                            {
                                continue;
                            }
                        }

                        ListItem objItem = new ListItem();
                        objItem.Value = objChoiceNode["name"]?.InnerText ?? string.Empty;
                        string strName = LanguageManager.Instance.GetString(objItem.Value, false);
                        if (string.IsNullOrEmpty(strName))
                            strName = LanguageManager.Instance.TranslateExtra(objItem.Value);
                        objItem.Name = strName;
                        lstGears.Add(objItem);
                    }

                    if (lstGears.Count <= 0)
                    {
                        if (objXmlChooseGearNode["required"]?.InnerText == "yes")
                        {
                            blnCancelledDialog = true;
                            break;
                        }
                        else
                            continue;
                    }

                    string strChooseGearNodeName = objXmlChooseGearNode["name"]?.InnerText ?? string.Empty;
                    string strFriendlyName = LanguageManager.Instance.GetString(strChooseGearNodeName, false);
                    if (string.IsNullOrEmpty(strFriendlyName))
                        strFriendlyName = LanguageManager.Instance.TranslateExtra(strChooseGearNodeName);
                    frmSelectItem frmPickItem = new frmSelectItem();
                    frmPickItem.Description = LanguageManager.Instance.GetString("String_Improvement_SelectText").Replace("{0}", strFriendlyName);
                    frmPickItem.GeneralItems = lstGears;

                    frmPickItem.ShowDialog();

                    // Make sure the dialogue window was not canceled.
                    if (frmPickItem.DialogResult == DialogResult.Cancel)
                    {
                        if (objXmlChooseGearNode["required"]?.InnerText == "yes")
                        {
                            blnCancelledDialog = true;
                            break;
                        }
                        else
                            continue;
                    }

                    XmlNode objXmlChosenGear = objXmlChooseGearNode.SelectSingleNode("usegear[name = \"" + frmPickItem.SelectedItem + "\"]");

                    if (objXmlChosenGear == null)
                    {
                        if (objXmlChooseGearNode["required"]?.InnerText == "yes")
                        {
                            blnCancelledDialog = true;
                            break;
                        }
                        else
                            continue;
                    }
                    else
                        lstChildrenToCreate.Add(objXmlChosenGear);
                }
                if (!blnCancelledDialog)
                {
                    foreach (XmlNode objXmlChild in lstChildrenToCreate)
                    {
                        CreateChild(objXmlGearDocument, objXmlChild, objParent, objNode, objCharacter, blnHacked, blnAddImprovements);
                    }
                }
            }

            if (!blnStartCollapsed && objNode.GetNodeCount(false) > 0)
                objNode.Expand();
        }

        protected void CreateChild(XmlDocument objXmlGearDocument, XmlNode objXmlChild, Gear objParent, TreeNode objNode, Character objCharacter, bool blnHacked, bool blnAddImprovements)
        {
            XmlNode objXmlGearNode = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlChild["name"].InnerText + "\" and category = \"" + objXmlChild["category"].InnerText + "\"]");
            if (objXmlGearNode == null)
                return;
            int intChildRating = 0;
            int intChildQty = 1;
            string strChildForceSource = string.Empty;
            string strChildForcePage = string.Empty;
            string strChildForceValue = string.Empty;
            bool blnCreateChildren = objXmlChild["name"].Attributes?["createchildren"]?.InnerText != "no";
            bool blnAddChildImprovements = blnAddImprovements;
            if (objXmlChild["name"].Attributes?["addimprovements"]?.InnerText == "no")
                blnAddChildImprovements = false;
            if (objXmlChild["rating"] != null)
                intChildRating = Convert.ToInt32(objXmlChild["rating"].InnerText);
            if (objXmlChild["name"].Attributes["qty"] != null)
                intChildQty = Convert.ToInt32(objXmlChild["name"].Attributes["qty"].InnerText);
            if (objXmlChild["name"].Attributes["select"] != null)
                strChildForceValue = objXmlChild["name"].Attributes["select"].InnerText;
            if (objXmlChild["source"] != null)
                strChildForceSource = objXmlChild["source"].InnerText;
            if (objXmlChild["page"] != null)
                strChildForcePage = objXmlChild["page"].InnerText;

            Gear objChild = new Gear(_objCharacter);
            TreeNode objChildNode = new TreeNode();
            List<Weapon> lstChildWeapons = new List<Weapon>();
            List<TreeNode> lstChildWeaponNodes = new List<TreeNode>();
            objChild.Create(objXmlGearNode, objCharacter, objChildNode, intChildRating, lstChildWeapons, lstChildWeaponNodes, strChildForceValue, blnHacked, false, true, blnCreateChildren);
            objChild.Quantity = intChildQty;
            objChild.Cost = "0";
            objChild.MinRating = intChildRating;
            objChild.MaxRating = intChildRating;
            objChild.Parent = objParent;
            objChild.IncludedInParent = true;
            if (!string.IsNullOrEmpty(strChildForceSource))
                objChild.Source = strChildForceSource;
            if (!string.IsNullOrEmpty(strChildForcePage))
                objChild.Page = strChildForcePage;
            objParent.Children.Add(objChild);
            if ((objParent as Commlink)?.CanSwapAttributes == true)
            {
                (objParent as Commlink).RefreshCyberdeckArray();
            }

            // Change the Capacity of the child if necessary.
            if (objXmlChild["capacity"] != null)
                objChild.Capacity = "[" + objXmlChild["capacity"].InnerText + "]";

            objNode.Nodes.Add(objChildNode);

            CreateChildren(objXmlGearDocument, objXmlChild, objChild, objChildNode, objCharacter, blnHacked, blnAddChildImprovements);
        }

        /// <summary>
        /// Copy a piece of Gear.
        /// </summary>
        /// <param name="objGear">Gear object to copy.</param>
        /// <param name="objNode">TreeNode for the copied item.</param>
        /// <param name="objWeapons">List of Weapons created by the copied item.</param>
        /// <param name="objWeaponNodes">List of TreeNodes for the Weapons created by the copied item.</param>
        public void Copy(Gear objGear, TreeNode objNode, List<Weapon> objWeapons, List<TreeNode> objWeaponNodes)
        {
            _SourceGuid = objGear._SourceGuid;
            _strName = objGear.Name;
            _strCategory = objGear.Category;
            _intMaxRating = objGear.MaxRating;
            _intMinRating = objGear.MinRating;
            _intRating = objGear.Rating;
            _intQty = objGear.Quantity;
            _strCapacity = objGear.Capacity;
            _strArmorCapacity = objGear.ArmorCapacity;
            _strAvail = objGear.Avail;
            _intCostFor = objGear.CostFor;
            _strDeviceRating = objGear.DeviceRating;
            _strCost = objGear.Cost;
            _strSource = objGear.Source;
            _strPage = objGear.Page;
            _strExtra = objGear.Extra;
            _blnBonded = objGear.Bonded;
            _blnEquipped = objGear.Equipped;
            _blnWirelessOn = objGear.WirelessOn;
            _blnHomeNode = objGear.HomeNode;
            _nodBonus = objGear.Bonus;
            _nodWirelessBonus = objGear.WirelessBonus;
            _nodWeaponBonus = objGear.WeaponBonus;
            _guiWeaponID = Guid.Parse(objGear.WeaponID);
            _strNotes = objGear.Notes;
            _strLocation = objGear.Location;
            _intChildAvailModifier = objGear.ChildAvailModifier;
            _intChildCostMultiplier = objGear.ChildCostMultiplier;
            _strGearName = objGear.GearName;

            objNode.Text = DisplayName;
            objNode.Tag = _guiID.ToString();

            foreach (Gear objGearChild in objGear.Children)
            {
                TreeNode objChildNode = new TreeNode();
                Gear objChild = new Gear(_objCharacter);
                if (objGearChild.GetType() == typeof(Commlink))
                {
                    Commlink objCommlink = new Commlink(_objCharacter);
                    objCommlink.Copy(objGearChild, objChildNode, objWeapons, objWeaponNodes);
                    objChild = objCommlink;
                }
                else
                    objChild.Copy(objGearChild, objChildNode, objWeapons, objWeaponNodes);
                _objChildren.Add(objChild);

                objNode.Nodes.Add(objChildNode);
                objNode.Expand();
            }
        }

        /// <summary>
        /// Begin to save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void SaveBegin(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("gear");
        }

        /// <summary>
        /// Core code to Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public virtual void SaveInner(XmlTextWriter objWriter)
        {
            objWriter.WriteElementString("guid", _guiID.ToString());
            objWriter.WriteElementString("id", _SourceGuid);
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("category", _strCategory);
            objWriter.WriteElementString("capacity", _strCapacity);
            objWriter.WriteElementString("armorcapacity", _strArmorCapacity);
            objWriter.WriteElementString("minrating", _intMinRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("maxrating", _intMaxRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("rating", _intRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("qty", _intQty.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("avail", _strAvail);
            if (_intCostFor > 1)
                objWriter.WriteElementString("costfor", _intCostFor.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("cost", _strCost);
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("bonded", _blnBonded.ToString());
            objWriter.WriteElementString("equipped", _blnEquipped.ToString());
            objWriter.WriteElementString("wirelesson", _blnWirelessOn.ToString());
            objWriter.WriteElementString("homenode", _blnHomeNode.ToString());
            if (_guiWeaponID != Guid.Empty)
                objWriter.WriteElementString("weaponguid", _guiWeaponID.ToString());
            if (_nodBonus != null)
                objWriter.WriteRaw("<bonus>" + _nodBonus.InnerXml + "</bonus>");
            else
                objWriter.WriteElementString("bonus", string.Empty);
            if (_nodWirelessBonus != null)
                objWriter.WriteRaw("<wirelessbonus>" + _nodWirelessBonus.InnerXml + "</wirelessbonus>");
            else
                objWriter.WriteElementString("wirelessbonus", string.Empty);
            if (_nodWeaponBonus != null)
                objWriter.WriteRaw("<weaponbonus>" + _nodWeaponBonus.InnerXml + "</weaponbonus>");
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("devicerating", _strDeviceRating);
            objWriter.WriteElementString("gearname", _strGearName);
            objWriter.WriteElementString("matrixcmfilled", _intMatrixCMFilled.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("matrixcmbonus", _intMatrixCMBonus.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("includedinparent", _blnIncludedInParent.ToString());
            if (_intChildCostMultiplier != 1)
                objWriter.WriteElementString("childcostmultiplier", _intChildCostMultiplier.ToString(CultureInfo.InvariantCulture));
            if (_intChildAvailModifier != 0)
                objWriter.WriteElementString("childavailmodifier", _intChildAvailModifier.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteStartElement("children");
            foreach (Gear objGear in _objChildren)
            {
                // Use the Gear's SubClass if applicable.
                if (objGear.GetType() == typeof(Commlink))
                {
                    Commlink objCommlink = objGear as Commlink;
                    objCommlink?.Save(objWriter);
                }
                else
                {
                    objGear.Save(objWriter);
                }
            }
            objWriter.WriteEndElement();
            objWriter.WriteElementString("location", _strLocation);
            objWriter.WriteElementString("notes", _strNotes);
            objWriter.WriteElementString("discountedcost", DiscountCost.ToString());
        }

        /// <summary>
        /// End saving the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void SaveEnd(XmlTextWriter objWriter)
        {
            objWriter.WriteEndElement();
            _objCharacter.SourceProcess(_strSource);
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlTextWriter objWriter)
        {
            SaveBegin(objWriter);
            SaveInner(objWriter);
            SaveEnd(objWriter);
        }

        /// <summary>
        /// Load the Gear from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public virtual void Load(XmlNode objNode, bool blnCopy = false)
        {
            _guiID = Guid.Parse(objNode["guid"].InnerText);
            objNode.TryGetStringFieldQuickly("id", ref _SourceGuid);
            objNode.TryGetStringFieldQuickly("name", ref _strName);
            objNode.TryGetStringFieldQuickly("category", ref _strCategory);
            objNode.TryGetInt32FieldQuickly("matrixcmfilled", ref _intMatrixCMFilled);
            objNode.TryGetInt32FieldQuickly("matrixcmbonus", ref _intMatrixCMBonus);
            objNode.TryGetStringFieldQuickly("capacity", ref _strCapacity);
            objNode.TryGetStringFieldQuickly("armorcapacity", ref _strArmorCapacity);
            objNode.TryGetInt32FieldQuickly("minrating", ref _intMinRating);
            objNode.TryGetInt32FieldQuickly("maxrating", ref _intMaxRating);
            objNode.TryGetInt32FieldQuickly("rating", ref _intRating);
            objNode.TryGetInt32FieldQuickly("qty", ref _intQty);
            objNode.TryGetStringFieldQuickly("avail", ref _strAvail);
            // Legacy shim
            if (string.IsNullOrEmpty(_strAvail) && (objNode["avail3"] != null || objNode["avail6"] != null || objNode["avail10"] != null))
            {
                _strAvail = MyXmlNode["avail"]?.InnerText ?? string.Empty;
            }
            objNode.TryGetInt32FieldQuickly("costfor", ref _intCostFor);
            objNode.TryGetStringFieldQuickly("cost", ref _strCost);
            // Legacy shim
            if (string.IsNullOrEmpty(_strCost) && (objNode["cost3"] != null || objNode["cost6"] != null || objNode["cost10"] != null))
            {
                _strCost = MyXmlNode["cost"]?.InnerText ?? string.Empty;
            }
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            if (_strExtra == "Hold-Outs")
                _strExtra = "Holdouts";
            objNode.TryGetBoolFieldQuickly("bonded", ref _blnBonded);
            objNode.TryGetBoolFieldQuickly("equipped", ref _blnEquipped);
            objNode.TryGetBoolFieldQuickly("homenode", ref _blnHomeNode);
            _nodBonus = objNode["bonus"];
            _nodWirelessBonus = objNode["wirelessbonus"];
            if (!objNode.TryGetBoolFieldQuickly("wirelesson", ref _blnWirelessOn))
                _blnWirelessOn = _nodWirelessBonus != null;
            _nodWeaponBonus = objNode["weaponbonus"];
            objNode.TryGetStringFieldQuickly("source", ref _strSource);
            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetStringFieldQuickly("devicerating", ref _strDeviceRating);
            string strWeaponID = string.Empty;
            if (objNode.TryGetStringFieldQuickly("weaponguid", ref strWeaponID))
            {
                _guiWeaponID = Guid.Parse(strWeaponID);
            }
            objNode.TryGetInt32FieldQuickly("childcostmultiplier", ref _intChildCostMultiplier);
            objNode.TryGetInt32FieldQuickly("childavailmodifier", ref _intChildAvailModifier);

            objNode.TryGetStringFieldQuickly("gearname", ref _strGearName);

            objNode.TryGetBoolFieldQuickly("includedinparent", ref _blnIncludedInParent);

            if (objNode.InnerXml.Contains("<gear>"))
            {
                XmlNodeList nodChildren = objNode.SelectNodes("children/gear");
                foreach (XmlNode nodChild in nodChildren)
                {
                    Gear objGear = new Gear(_objCharacter);
                    objGear.Load(nodChild, blnCopy);

                    if (objGear.MyXmlNode["devicerating"] != null)
                    {
                        Commlink objCommlink = new Commlink(_objCharacter);
                        objCommlink.Load(nodChild, blnCopy);
                        objGear = objCommlink;
                    }
                    objGear.Parent = this;
                    _objChildren.Add(objGear);
                }
            }

            objNode.TryGetStringFieldQuickly("location", ref _strLocation);
            objNode.TryGetStringFieldQuickly("notes", ref _strNotes);

            objNode.TryGetBoolFieldQuickly("discountedcost", ref _blnDiscountCost);

            if (GlobalOptions.Instance.Language != "en-us")
            {
                XmlNode objGearNode = MyXmlNode;
                if (objGearNode != null)
                {
                    objGearNode.TryGetStringFieldQuickly("translate", ref _strAltName);
                    objGearNode.TryGetStringFieldQuickly("altpage", ref _strAltPage);
                }

                if (_strAltName.StartsWith("Stacked Focus"))
                    _strAltName = _strAltName.Replace("Stacked Focus", LanguageManager.Instance.GetString("String_StackedFocus"));

                XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
                objGearNode = objXmlDocument?.SelectSingleNode("/chummer/categories/category[. = \"" + _strCategory + "\"]");
                objGearNode?.TryGetStringFieldQuickly("translate", ref _strAltCategory);

                if (_strAltCategory.StartsWith("Stacked Focus"))
                    _strAltCategory = _strAltCategory.Replace("Stacked Focus", LanguageManager.Instance.GetString("String_StackedFocus"));
            }

            // Convert old qi foci to the new bonus. In order to force the user to update their powers, unequip the focus and remove all improvements.
            if (_strName == "Qi Focus")
            {
                Version.TryParse("5.193.5", out Version test);
                if (test != null)
                {
                    int intResult = _objCharacter.LastSavedVersion.CompareTo(test);
                    //Check for typo in Corrupter quality and correct it
                    if (intResult == -1)
                    {
                        XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
                        XmlNode gear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + _strName + "\"]");
                        if (gear != null)
                        {
                            Equipped = false;
                            ImprovementManager.RemoveImprovements(_objCharacter, Improvement.ImprovementSource.Gear, InternalId);
                            Bonus = gear["bonus"];
                            WirelessBonus = gear["wirelessbonus"];
                        }
                    }
                }
            }
            if (blnCopy)
            {
                _guiID = Guid.NewGuid();
                _strLocation = string.Empty;
                _blnHomeNode = false;
            }
        }

        /// <summary>
        /// Begin Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void PrintBegin(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("gear");
        }

        /// <summary>
        /// Core code to Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public virtual void PrintInner(XmlTextWriter objWriter, bool blnIsCommlink = false, bool blnIsPersona = false)
        {
            if ((_strCategory == "Foci" || _strCategory == "Metamagic Foci") && _blnBonded)
            {
                objWriter.WriteElementString("name", DisplayNameShort + " (" + LanguageManager.Instance.GetString("Label_BondedFoci") + ")");
            }
            else
                objWriter.WriteElementString("name", DisplayNameShort);
            objWriter.WriteElementString("name_english", _strName);
            objWriter.WriteElementString("category", DisplayCategory);
            objWriter.WriteElementString("category_english", _strCategory);
            objWriter.WriteElementString("iscommlink", blnIsCommlink.ToString());
            objWriter.WriteElementString("ispersona", blnIsPersona.ToString());
            //objWriter.WriteElementString("isnexus", (_strCategory == "Nexus").ToString());
            objWriter.WriteElementString("isammo", (_strCategory == "Ammunition").ToString());
            objWriter.WriteElementString("isprogram", IsProgram.ToString());
            objWriter.WriteElementString("isos", false.ToString());
            if (_strName == "Fake SIN")
                objWriter.WriteElementString("issin", true.ToString());
            else
                objWriter.WriteElementString("issin", false.ToString());
            objWriter.WriteElementString("capacity", _strCapacity);
            objWriter.WriteElementString("armorcapacity", _strArmorCapacity);
            objWriter.WriteElementString("maxrating", _intMaxRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("rating", _intRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("matrixcmfilled", _intMatrixCMFilled.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("conditionmonitor", MatrixCM.ToString());
            objWriter.WriteElementString("qty", _intQty.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("avail", TotalAvail(true));
            objWriter.WriteElementString("avail_english", TotalAvail(true, true));
            objWriter.WriteElementString("cost", TotalCost.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("owncost", OwnCost.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("extra", LanguageManager.Instance.TranslateExtra(_strExtra));
            objWriter.WriteElementString("bonded", _blnBonded.ToString());
            objWriter.WriteElementString("equipped", _blnEquipped.ToString());
            objWriter.WriteElementString("wirelesson", _blnWirelessOn.ToString());
            objWriter.WriteElementString("homenode", _blnHomeNode.ToString());
            objWriter.WriteElementString("location", _strLocation);
            objWriter.WriteElementString("gearname", _strGearName);
            objWriter.WriteElementString("source", _objCharacter.Options.LanguageBookShort(_strSource));
            objWriter.WriteElementString("page", Page);
            objWriter.WriteStartElement("children");
            foreach (Gear objGear in _objChildren)
            {
                // Use the Gear's SubClass if applicable.
                if (objGear.GetType() == typeof(Commlink))
                {
                    Commlink objCommlink = objGear as Commlink;
                    objCommlink?.Print(objWriter);
                }
                else
                {
                    objGear.Print(objWriter);
                }
            }
            objWriter.WriteEndElement();
            if (_nodWeaponBonus != null)
            {
                objWriter.WriteElementString("weaponbonusdamage", WeaponBonusDamage());
                objWriter.WriteElementString("weaponbonusdamage_english", WeaponBonusDamage(true));
                objWriter.WriteElementString("weaponbonusap", WeaponBonusAP);
            }
            if (_objCharacter.Options.PrintNotes)
                objWriter.WriteElementString("notes", _strNotes);
        }

        /// <summary>
        /// End Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void PrintEnd(XmlTextWriter objWriter)
        {
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Print(XmlTextWriter objWriter)
        {
            PrintBegin(objWriter);
            PrintInner(objWriter);
            PrintEnd(objWriter);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Internal identifier which will be used to identify this piece of Gear in the Character.
        /// </summary>
        public string InternalId
        {
            get
            {
                return _guiID.ToString();
            }
        }
        public string SourceID
        {
            get
            {
                return _SourceGuid;
            }
        }
        /// <summary>
        /// Whether or not an item is an A.I.'s Home Node.
        /// </summary>
        public bool HomeNode
        {
            get
            {
                return _blnHomeNode;
            }
            set
            {
                _blnHomeNode = value;
            }
        }

        /// <summary>
        /// Guid of a Cyberware Weapon.
        /// </summary>
        public string WeaponID
        {
            get
            {
                return _guiWeaponID.ToString();
            }
            set
            {
                _guiWeaponID = Guid.Parse(value);
            }
        }

        /// <summary>
        /// Bonus node from the XML file.
        /// </summary>
        public XmlNode Bonus
        {
            get
            {
                return _nodBonus;
            }
            set
            {
                _nodBonus = value;
            }
        }

        /// <summary>
        /// Wireless bonus node from the XML file.
        /// </summary>
        public XmlNode WirelessBonus
        {
            get
            {
                return _nodWirelessBonus;
            }
            set
            {
                _nodWirelessBonus = value;
            }
        }

        /// <summary>
        /// WeaponBonus node from the XML file.
        /// </summary>
        public XmlNode WeaponBonus
        {
            get
            {
                return _nodWeaponBonus;
            }
            set
            {
                _nodWeaponBonus = value;
            }
        }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _strName;
            }
            set
            {
                _strName = value;
            }
        }

        /// <summary>
        /// A custom name for the Gear assigned by the player.
        /// </summary>
        public string GearName
        {
            get
            {
                return _strGearName;
            }
            set
            {
                _strGearName = value;
            }
        }

        /// <summary>
        /// Translated Category.
        /// </summary>
        public string DisplayCategory
        {
            get
            {
                if (!string.IsNullOrEmpty(_strAltCategory))
                    return _strAltCategory;

                return _strCategory;
            }
        }

        /// <summary>
        /// Category.
        /// </summary>
        public string Category
        {
            get
            {
                return _strCategory;
            }
            set
            {
                _strCategory = value;
            }
        }

        /// <summary>
        /// Gear capacity.
        /// </summary>
        public string Capacity
        {
            get
            {
                return _strCapacity;
            }
            set
            {
                _strCapacity = value;
            }
        }

        /// <summary>
        /// Armor capacity.
        /// </summary>
        public string ArmorCapacity
        {
            get
            {
                return _strArmorCapacity;
            }
            set
            {
                _strArmorCapacity = value;
            }
        }

        /// <summary>
        /// Minimum Rating.
        /// </summary>
        public int MinRating
        {
            get
            {
                return _intMinRating;
            }
            set
            {
                _intMinRating = value;
            }
        }

        /// <summary>
        /// Maximum Rating.
        /// </summary>
        public int MaxRating
        {
            get
            {
                return _intMaxRating;
            }
            set
            {
                _intMaxRating = value;
            }
        }

        /// <summary>
        /// Rating.
        /// </summary>
        public int Rating
        {
            get
            {
                return _intRating;
            }
            set
            {
                _intRating = value;
                // Make sure we don't go over the maximum Rating.
                if (_intRating > _intMaxRating)
                    _intRating = _intMaxRating;
            }
        }

        /// <summary>
        /// Quantity.
        /// </summary>
        public int Quantity
        {
            get
            {
                return _intQty;
            }
            set
            {
                _intQty = value;
            }
        }

        /// <summary>
        /// Availability.
        /// </summary>
        public string Avail
        {
            get
            {
                return _strAvail;
            }
            set
            {
                _strAvail = value;
            }
        }

        /// <summary>
        /// Use for ammo. The number of rounds that the nuyen amount buys.
        /// </summary>
        public int CostFor
        {
            get
            {
                return _intCostFor;
            }
            set
            {
                _intCostFor = value;
            }
        }

        /// <summary>
        /// Cost.
        /// </summary>
        public string Cost
        {
            get
            {
                if (_strCost.StartsWith("FixedValues"))
                {
                    string[] strValues = _strCost.Replace("FixedValues", string.Empty).Trim("()".ToCharArray()).Split(',');
                    string strCost = "0";
                    if (_intRating > 0)
                        strCost = strValues[Math.Min(_intRating, strValues.Length) - 1].Replace("[", string.Empty).Replace("]", string.Empty);
                    else
                        strCost = strValues[0].Replace("[", string.Empty).Replace("]", string.Empty);
                    return strCost;
                }
                else if (_strCost.StartsWith("Parent Cost"))
                {

                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();

                    string strCostExpression = _strCost;
                    string strCost = "0";

                    if (_objParent == null)
                    {
                        return strCost;
                    }
                    else
                    {
                        strCost = strCostExpression.Replace("Weapon Cost", _objParent.Cost);
                    }
                    return strCost;
                }
                else
                    return _strCost;
            }
            set
            {
                _strCost = value;
            }
        }

        /// <summary>
        /// Value that was selected during an ImprovementManager dialogue.
        /// </summary>
        public string Extra
        {
            get
            {
                return _strExtra;
            }
            set
            {
                _strExtra = value;
            }
        }

        /// <summary>
        /// Whether or not the Foci is bonded.
        /// </summary>
        public bool Bonded
        {
            get
            {
                return _blnBonded;
            }
            set
            {
                _blnBonded = value;
            }
        }

        /// <summary>
        /// Whether or not the Gear is equipped.
        /// </summary>
        public bool Equipped
        {
            get
            {
                return _blnEquipped;
            }
            set
            {
                _blnEquipped = value;
            }
        }

        /// <summary>
        /// Whether or not the Gear's wireless bonus is enabled.
        /// </summary>
        public bool WirelessOn
        {
            get
            {
                return _blnWirelessOn;
            }
            set
            {
                _blnWirelessOn = value;
            }
        }

        /// <summary>
        /// Sourcebook.
        /// </summary>
        public string Source
        {
            get
            {
                return _strSource;
            }
            set
            {
                _strSource = value;
            }
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page
        {
            get
            {
                if (!string.IsNullOrEmpty(_strAltPage))
                    return _strAltPage;

                return _strPage;
            }
            set
            {
                _strPage = value;
            }
        }

        /// <summary>
        /// A List of child pieces of Gear.
        /// </summary>
        public List<Gear> Children
        {
            get
            {
                return _objChildren;
            }
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get
            {
                return _strNotes;
            }
            set
            {
                _strNotes = value;
            }
        }

        /// <summary>
        /// Device Rating string.
        /// </summary>
        public string DeviceRating
        {
            get
            {
                return _strDeviceRating;
            }
            set
            {
                _strDeviceRating = value;
            }
        }

        /// <summary>
        /// Total Device Rating of the gear.
        /// </summary>
        public int TotalDeviceRating
        {
            get
            {
                if (string.IsNullOrEmpty(_strDeviceRating))
                    return 2;
                int intReturn = 0;

                string strExpression = _strDeviceRating;

                if (strExpression.StartsWith("FixedValues"))
                {
                    string[] strValues = strExpression.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                    if (_intRating > 0)
                        strExpression = strValues[Math.Min(_intRating, strValues.Length) - 1].Replace("[", string.Empty).Replace("]", string.Empty);
                }

                int intGearValue = 0;
                string strParentValue = string.Empty;
                if (_objParent != null)
                {
                    if (strExpression.Contains("Gear DR"))
                        intGearValue = _objParent.TotalDeviceRating;
                    if (strExpression.Contains("Parent DR"))
                        strParentValue = _objParent.DeviceRating;
                }
                int intTotalChildrenValue = 0;
                if (_objChildren.Count > 0 && strExpression.Contains("Children DR"))
                {
                    foreach (Gear loopGear in _objChildren.Where(x => x.Equipped))
                    {
                        intTotalChildrenValue += loopGear.TotalDeviceRating;
                    }
                }

                if (intGearValue != 0 || intTotalChildrenValue != 0 || !string.IsNullOrEmpty(strParentValue) || strExpression.Contains("Rating"))
                {
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();
                    string strValue = strExpression.Replace("Gear DR", intGearValue.ToString(GlobalOptions.InvariantCultureInfo));
                    strValue = strValue.Replace("Children DR", intTotalChildrenValue.ToString(GlobalOptions.InvariantCultureInfo));
                    strValue = strValue.Replace("Rating", _intRating.ToString(GlobalOptions.InvariantCultureInfo));
                    strValue = strValue.Replace("Parent DR", string.IsNullOrEmpty(strParentValue) ? "0" : strParentValue);
                    XPathExpression xprCost = nav.Compile(strValue);
                    // This is first converted to a double and rounded up since some items have a multiplier that is not a whole number, such as 2.5.
                    double dblValue = Math.Ceiling(Convert.ToDouble(nav.Evaluate(xprCost), GlobalOptions.InvariantCultureInfo));
                    intReturn = Convert.ToInt32(dblValue);
                }
                else
                {
                    // Just a straight cost, so return the value.
                    intReturn = Convert.ToInt32(strExpression);
                }

                return intReturn;
            }
        }

        /// <summary>
        /// Location.
        /// </summary>
        public string Location
        {
            get
            {
                return _strLocation;
            }
            set
            {
                _strLocation = value;
            }
        }

        /// <summary>
        /// Whether or not the Gear qualifies as a Program in the printout XML.
        /// </summary>
        public bool IsProgram
        {
            get
            {
                if (_strCategory == "ARE Programs" || _strCategory.StartsWith("Autosofts") || _strCategory == "Data Software" || _strCategory == "Malware" || _strCategory == "Matrix Programs" || _strCategory == "Tactical AR Software" || _strCategory == "Telematics Infrastructure Software" || _strCategory == "Sensor Software")
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Whether or not the Gear has the Ergonomic Program Option.
        /// </summary>
        public bool IsErgonomic
        {
            get
            {
                foreach (Gear objPlugin in _objChildren)
                {
                    if (objPlugin.Name == "Ergonomic")
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Cost multiplier for Children attached to this Gear.
        /// </summary>
        public int ChildCostMultiplier
        {
            get
            {
                return _intChildCostMultiplier;
            }
            set
            {
                _intChildCostMultiplier = value;
            }
        }

        /// <summary>
        /// Avail modifier for Children attached to this Gear.
        /// </summary>
        public int ChildAvailModifier
        {
            get
            {
                return _intChildAvailModifier;
            }
            set
            {
                _intChildAvailModifier = value;
            }
        }

        /// <summary>
        /// Parent Gear.
        /// </summary>
        public Gear Parent
        {
            get
            {
                return _objParent;
            }
            set
            {
                _objParent = value;
            }
        }

        /// <summary>
        /// Whether or not the Gear's cost should be discounted by 10% through the Black Market Pipeline Quality.
        /// </summary>
        public bool DiscountCost
        {
            get
            {
                return _blnDiscountCost;
            }
            set
            {
                _blnDiscountCost = value;
            }
        }

        /// <summary>
        /// Whether or not the Gear is included in its parent item when purchased (currently applies to Armor only).
        /// </summary>
        public bool IncludedInParent
        {
            get
            {
                return _blnIncludedInParent;
            }
            set
            {
                _blnIncludedInParent = value;
            }
        }

        public XmlNode MyXmlNode
        {
            get
            {
                return XmlManager.Instance.Load("gear.xml")?.SelectSingleNode("/chummer/gears/gear[(id = \"" + _SourceGuid + "\") or (name = \"" + Name + "\" and category = \"" + Category + "\")]");
            }
        }
        #endregion

        #region Complex Properties
        /// <summary>
        /// Total Availablility of the Gear and its accessories.
        /// </summary>
        public string TotalAvail(bool blnCalculateAdditions = false, bool blnForceEnglish = false)
        {
            if (string.IsNullOrEmpty(_strAvail))
                _strAvail = "0";

            bool blnIncludePlus = false;

            // If the Avail contains "+", return the base string and don't try to calculate anything since we're looking at a child component.
            if (_strAvail.StartsWith("+"))
            {
                if (!blnCalculateAdditions)
                    return _strAvail;
                blnIncludePlus = true;
            }

            string strCalculated;

            string strAvailExpression = _strAvail;

            if (strAvailExpression.Contains("Rating"))
            {
                // If the availability is determined by the Rating, evaluate the expression.
                XmlDocument objXmlDocument = new XmlDocument();
                XPathNavigator nav = objXmlDocument.CreateNavigator();

                string strAvail = string.Empty;
                if (blnIncludePlus)
                    strAvailExpression = strAvailExpression.Substring(1, strAvailExpression.Length - 1);

                if (strAvailExpression.Substring(strAvailExpression.Length - 1, 1) == "F" || strAvailExpression.Substring(strAvailExpression.Length - 1, 1) == "R")
                {
                    strAvail = strAvailExpression.Substring(strAvailExpression.Length - 1, 1);
                    // Remove the trailing character if it is "F" or "R".
                    strAvailExpression = strAvailExpression.Substring(0, strAvailExpression.Length - 1);
                }
                XPathExpression xprAvail = nav.Compile(strAvailExpression.Replace("Rating", _intRating.ToString(CultureInfo.InvariantCulture)));
                strCalculated = Convert.ToInt32(nav.Evaluate(xprAvail)) + strAvail;
            }
            else
            {
                // Just a straight cost, so return the value.
                strCalculated = strAvailExpression.Contains("F") || strAvailExpression.Contains("R")
                    ? Convert.ToInt32(strAvailExpression.Substring(0, strAvailExpression.Length - 1)) + strAvailExpression.Substring(strAvailExpression.Length - 1, 1)
                    : Convert.ToInt32(strAvailExpression).ToString();
            }

            int intAvail;
            string strAvailText = string.Empty;
            if (strCalculated.Contains("F") || strCalculated.Contains("R"))
            {
                strAvailText = strCalculated.Substring(strCalculated.Length - 1);
                intAvail = Convert.ToInt32(strCalculated.Replace(strAvailText, string.Empty));
            }
            else
                intAvail = Convert.ToInt32(strCalculated);

            // Run through the child items and increase the Avail by any Mod whose Avail contains "+".
            foreach (Gear objChild in _objChildren)
            {
                if (objChild.Avail.StartsWith("+"))
                {
                    string strAvail = objChild.Avail.Replace("Rating", objChild.Rating.ToString());
                    strAvail = strAvail.Substring(1).Trim();
                    if (strAvail.Contains("R") || strAvail.Contains("F"))
                    {
                        if (strAvailText != "F")
                            strAvailText = strAvail.Substring(strAvail.Length - 1);
                        XmlDocument objXmlDocument = new XmlDocument();
                        XPathNavigator nav = objXmlDocument.CreateNavigator();
                        XPathExpression xprAvail = nav.Compile(strAvail.Replace("F", string.Empty).Replace("R", string.Empty));
                        intAvail += Convert.ToInt32(nav.Evaluate(xprAvail));
                    }
                    else
                    {
                        XmlDocument objXmlDocument = new XmlDocument();
                        XPathNavigator nav = objXmlDocument.CreateNavigator();
                        XPathExpression xprAvail = nav.Compile(strAvail.Replace("F", string.Empty).Replace("R", string.Empty));
                        intAvail += Convert.ToInt32(nav.Evaluate(xprAvail));
                    }
                }
            }

            // Add any Avail modifier that comes from its Parent.
            if (_objParent != null)
                intAvail += _objParent.ChildAvailModifier;

            string strReturn = intAvail + strAvailText;

            // Translate the Avail string.
            if (!blnForceEnglish)
            {
                strReturn = strReturn.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted"));
                strReturn = strReturn.Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
            }

            if (blnIncludePlus)
                strReturn = "+" + strReturn;

            return strReturn;
        }

        /// <summary>
        /// Caculated Capacity of the Gear.
        /// </summary>
        public string CalculatedCapacity
        {
            get
            {
                if (_strCapacity.Contains("/["))
                {
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();

                    int intPos = _strCapacity.IndexOf("/[");
                    string strFirstHalf = _strCapacity.Substring(0, intPos);
                    string strSecondHalf = _strCapacity.Substring(intPos + 1, _strCapacity.Length - intPos - 1);
                    bool blnSquareBrackets = strFirstHalf.Contains('[');
                    string strCapacity = strFirstHalf;
                    if (blnSquareBrackets && strCapacity.Length > 2)
                        strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                    XPathExpression xprCapacity = nav.Compile(strCapacity.Replace("Rating", _intRating.ToString()));

                    string strReturn;
                    if (_strArmorCapacity == "[*]")
                        strReturn = "*";
                    else if (_strArmorCapacity.StartsWith("FixedValues"))
                    {
                        char[] chrParentheses = { '(', ')' };
                        string[] strValues = _strArmorCapacity.Replace("FixedValues", string.Empty).Trim(chrParentheses).Split(',');
                        strReturn = strValues[Math.Min(_intRating, strValues.Length) - 1];
                    }
                    else
                        strReturn = nav.Evaluate(xprCapacity).ToString();
                    strReturn += "/" + strSecondHalf;
                    return strReturn;
                }
                if (_strCapacity.Contains("Rating"))
                {
                    // If the Capaicty is determined by the Rating, evaluate the expression.
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();

                    // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
                    bool blnSquareBrackets = _strCapacity.Contains('[');
                    string strCapacity = _strCapacity;
                    if (blnSquareBrackets)
                        strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                    XPathExpression xprCapacity = nav.Compile(strCapacity.Replace("Rating", _intRating.ToString()));

                    // This has resulted in a non-whole number, so round it (minimum of 1).
                    decimal decNumber = Convert.ToDecimal(nav.Evaluate(xprCapacity), GlobalOptions.InvariantCultureInfo);
                    int intNumber = Convert.ToInt32(Math.Ceiling(decNumber));
                    if (intNumber < 1)
                        intNumber = 1;
                    string strReturn = intNumber.ToString();

                    if (blnSquareBrackets)
                        strReturn = "[" + strReturn + "]";

                    return strReturn;
                }
                // Just a straight Capacity, so return the value.
                return string.IsNullOrEmpty(_strCapacity) ? "0" : _strCapacity;
            }
        }

        /// <summary>
        /// Caculated Capacity of the Gear when attached to Armor.
        /// </summary>
        public string CalculatedArmorCapacity
        {
            get
            {
                if (_strArmorCapacity.Contains("/["))
                {
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();

                    int intPos = _strArmorCapacity.IndexOf("/[");
                    string strFirstHalf = _strArmorCapacity.Substring(0, intPos);
                    string strSecondHalf = _strArmorCapacity.Substring(intPos + 1, _strArmorCapacity.Length - intPos - 1);
                    bool blnSquareBrackets = strFirstHalf.Contains('[');
                    string strCapacity = strFirstHalf;
                    if (blnSquareBrackets && strCapacity.Length > 2)
                        strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);

                    XPathExpression xprCapacity = nav.Compile(strCapacity.Replace("Rating", _intRating.ToString()));

                    string strReturn;
                    if (_strArmorCapacity == "[*]")
                        strReturn = "*";
                    else if(_strArmorCapacity.StartsWith("FixedValues"))
                    {
                        char[] chrParentheses = { '(', ')' };
                        string[] strValues = _strArmorCapacity.Replace("FixedValues", string.Empty).Trim(chrParentheses).Split(',');
                        strReturn = strValues[Math.Min(_intRating, strValues.Length) - 1];
                    }
                    else
                        strReturn = nav.Evaluate(xprCapacity).ToString();
                    if (blnSquareBrackets)
                        strReturn = "[" + strCapacity + "]";
                    strReturn += "/" + strSecondHalf;
                    return strReturn;
                }
                else if (_strArmorCapacity.Contains("Rating"))
                {
                    // If the Capaicty is determined by the Rating, evaluate the expression.
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();

                    // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
                    bool blnSquareBrackets = _strArmorCapacity.Contains('[');
                    string strCapacity = _strArmorCapacity;
                    if (blnSquareBrackets)
                        strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                    XPathExpression xprCapacity = nav.Compile(strCapacity.Replace("Rating", _intRating.ToString()));

                    string strReturn = nav.Evaluate(xprCapacity).ToString();
                    if (blnSquareBrackets)
                        strReturn = "[" + strReturn + "]";

                    return strReturn;
                }
                // Just a straight Capacity, so return the value.
                else if (string.IsNullOrEmpty(_strArmorCapacity))
                    return "0";
                else
                    return _strArmorCapacity;
            }
        }

        /// <summary>
        /// Total cost of the just the Gear itself before we factor in any multipliers.
        /// </summary>
        public int OwnCostPreMultipliers
        {
            get
            {
                int intReturn;

                string strCostExpression = _strCost;

                if (strCostExpression.StartsWith("FixedValues"))
                {
                    string[] strValues = strCostExpression.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                    if (_intRating > 0)
                        strCostExpression = strValues[Math.Min(_intRating, strValues.Length) - 1].Replace("[", string.Empty).Replace("]", string.Empty);
                }

                int intGearCost = 0;
                string strParentCost = string.Empty;
                if (_objParent != null)
                {
                    if (strCostExpression.Contains("Gear Cost"))
                        intGearCost = _objParent.CalculatedCost;
                    if (strCostExpression.Contains("Parent Cost"))
                        strParentCost = _objParent.Cost;
                }
                int intTotalChildrenCost = 0;
                if (_objChildren.Count > 0 && strCostExpression.Contains("Children Cost"))
                {
                    foreach (Gear loopGear in _objChildren)
                    {
                        intTotalChildrenCost += loopGear.CalculatedCost;
                    }
                }

                if (string.IsNullOrEmpty(strCostExpression))
                    return 0;

                if (intGearCost != 0 || intTotalChildrenCost != 0 || !string.IsNullOrEmpty(strParentCost) || strCostExpression.Contains("Rating"))
                {
                    XmlDocument objXmlDocument = new XmlDocument();
                    XPathNavigator nav = objXmlDocument.CreateNavigator();
                    string strCost = strCostExpression.Replace("Gear Cost", intGearCost.ToString(GlobalOptions.InvariantCultureInfo));
                    strCost = strCost.Replace("Children Cost", intTotalChildrenCost.ToString(GlobalOptions.InvariantCultureInfo));
                    strCost = strCost.Replace("Rating", _intRating.ToString(GlobalOptions.InvariantCultureInfo));
                    strCost = strCost.Replace("Parent Cost", string.IsNullOrEmpty(strParentCost) ? "0" : strParentCost);
                    XPathExpression xprCost = nav.Compile(strCost);
                    // This is first converted to a double and rounded up since some items have a multiplier that is not a whole number, such as 2.5.
                    double dblCost = Math.Ceiling(Convert.ToDouble(nav.Evaluate(xprCost), GlobalOptions.InvariantCultureInfo));
                    intReturn = Convert.ToInt32(dblCost);
                }
                else
                {
                    // Just a straight cost, so return the value.
                    intReturn = Convert.ToInt32(strCostExpression);
                }
                return intReturn;
            }
        }

        /// <summary>
        /// Total cost of the just the Gear itself.
        /// </summary>
        public int CalculatedCost
        {
            get
            {
                // The number is divided at the end for ammo purposes. This is done since the cost is per "costfor" but is being multiplied by the actual number of rounds.
                return (OwnCostPreMultipliers * _intQty) / _intCostFor;
            }
        }

        /// <summary>
        /// Total cost of the Gear and its accessories.
        /// </summary>
        public int TotalCost
        {
            get
            {
                int intReturn = OwnCostPreMultipliers;

                if (DiscountCost)
                    intReturn = intReturn * 9 / 10;

                // Add in the cost of all child components.
                int intPlugin = 0;
                foreach (Gear objChild in _objChildren)
                    intPlugin += objChild.TotalCost;

                // The number is divided at the end for ammo purposes. This is done since the cost is per "costfor" but is being multiplied by the actual number of rounds.
                int intParentMultiplier = 1;
                if (_objParent != null)
                    intParentMultiplier = _objParent.ChildCostMultiplier;

                intReturn = (intReturn * _intQty * intParentMultiplier) / _intCostFor;
                // Add in the cost of the plugins separate since their value is not based on the Cost For number (it is always cost x qty).
                intReturn += intPlugin * _intQty;

                return intReturn;
            }
        }

        /// <summary>
        /// The cost of just the Gear itself.
        /// </summary>
        public int OwnCost
        {
            get
            {
                int intReturn = OwnCostPreMultipliers;

                if (DiscountCost)
                    intReturn = intReturn * 9 / 10;

                // The number is divided at the end for ammo purposes. This is done since the cost is per "costfor" but is being multiplied by the actual number of rounds.
                int intParentMultiplier = 1;
                if (_objParent != null)
                    intParentMultiplier = _objParent.ChildCostMultiplier;

                intReturn = (intReturn * intParentMultiplier) / _intCostFor;

                return intReturn;
            }
        }

        /// <summary>
        /// The Gear's Capacity cost if used as a plugin.
        /// </summary>
        public int PluginCapacity
        {
            get
            {
                string strCapacity = CalculatedCapacity;
                if (strCapacity.Contains("/["))
                {
                    // If this is a multiple-capacity item, use only the second half.
                    int intPos = strCapacity.IndexOf("/[");
                    strCapacity = strCapacity.Substring(intPos + 1);
                }

                // Only items that contain square brackets should consume Capacity. Everything else is treated as [0].
                if (strCapacity.Contains("["))
                    strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                else
                    strCapacity = "0";
                return Convert.ToInt32(strCapacity);
            }
        }

        /// <summary>
        /// The Gear's Capacity cost if used as an Armor plugin.
        /// </summary>
        public int PluginArmorCapacity
        {
            get
            {
                string strCapacity = CalculatedArmorCapacity;
                if (strCapacity.Contains("/["))
                {
                    // If this is a multiple-capacity item, use only the second half.
                    int intPos = strCapacity.IndexOf("/[");
                    strCapacity = strCapacity.Substring(intPos + 1);
                }

                // Only items that contain square brackets should consume Capacity. Everything else is treated as [0].
                if (strCapacity.Contains("["))
                    strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                else
                    strCapacity = "0";
                return Convert.ToInt32(strCapacity);
            }
        }

        /// <summary>
        /// The amount of Capacity remaining in the Gear.
        /// </summary>
        public int CapacityRemaining
        {
            get
            {
                int intCapacity = 0;
                if (!_strCapacity.Contains("[") || _strCapacity.Contains("/["))
                {
                    // Get the Gear base Capacity.
                    if (_strCapacity.Contains("/["))
                    {
                        // If this is a multiple-capacity item, use only the first half.
                        string strMyCapacity = CalculatedCapacity;
                        int intPos = strMyCapacity.IndexOf("/[");
                        strMyCapacity = strMyCapacity.Substring(0, intPos);
                        intCapacity = Convert.ToInt32(strMyCapacity);
                    }
                    else
                        intCapacity = Convert.ToInt32(CalculatedCapacity);

                    // Run through its Children and deduct the Capacity costs.
                    foreach (Gear objChildGear in Children)
                    {
                        string strCapacity = objChildGear.CalculatedCapacity;
                        if (strCapacity.Contains("/["))
                        {
                            // If this is a multiple-capacity item, use only the second half.
                            int intPos = strCapacity.IndexOf("/[");
                            strCapacity = strCapacity.Substring(intPos + 1);
                        }

                        // Only items that contain square brackets should consume Capacity. Everything else is treated as [0].
                        if (strCapacity.Contains("["))
                            strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                        else
                            strCapacity = "0";
                        intCapacity -= (Convert.ToInt32(strCapacity) * objChildGear.Quantity);
                    }
                }

                return intCapacity;
            }
        }

        /// <summary>
        /// The name of the object as it should appear on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort
        {
            get
            {
                return (string.IsNullOrEmpty(_strAltName) ? _strName : _strAltName);
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Qty Name (Rating) (Extra).
        /// </summary>
        public string DisplayName
        {
            get
            {
                string strReturn = DisplayNameShort;

                if (_intQty > 1)
                    strReturn = _intQty + " " + strReturn;
                if (_intRating > 0)
                    strReturn += " (" + LanguageManager.Instance.GetString("String_Rating") + " " + _intRating + ")";
                if (!string.IsNullOrEmpty(_strExtra))
                    strReturn += " (" + LanguageManager.Instance.TranslateExtra(_strExtra) + ")";

                if (!string.IsNullOrEmpty(_strGearName))
                {
                    strReturn += " (\"" + _strGearName + "\")";
                }

                return strReturn;
            }
        }

        /// <summary>
        /// Weapon Bonus Damage.
        /// </summary>
        public string WeaponBonusDamage(bool blnForceEnglish = false)
        {
            if (_nodWeaponBonus == null)
                return string.Empty;
            else
            {
                string strReturn = "0";
                // Use the damagereplace value if applicable.
                if (_nodWeaponBonus["damagereplace"] != null)
                    strReturn = _nodWeaponBonus["damagereplace"].InnerText;
                else
                {
                    // Use the damage bonus if available, otherwise use 0.
                    if (_nodWeaponBonus["damage"] != null)
                        strReturn = _nodWeaponBonus["damage"].InnerText;

                    // Attach the type if applicable.
                    if (_nodWeaponBonus["damagetype"] != null)
                        strReturn += _nodWeaponBonus["damagetype"].InnerText;

                    // If this does not start with "-", add a "+" to the string.
                    if (!strReturn.StartsWith("-"))
                        strReturn = "+" + strReturn;
                }

                // Translate the Avail string.
                if (!blnForceEnglish)
                {
                    strReturn = strReturn.Replace("P", LanguageManager.Instance.GetString("String_DamagePhysical"));
                    strReturn = strReturn.Replace("S", LanguageManager.Instance.GetString("String_DamageStun"));
                }

                return strReturn;
            }
        }

        /// <summary>
        /// Weapon Bonus AP.
        /// </summary>
        public string WeaponBonusAP
        {
            get
            {
                if (_nodWeaponBonus == null)
                    return string.Empty;
                else
                {
                    string strReturn = "0";
                    // Use the apreplace value if applicable.
                    if (_nodWeaponBonus["apreplace"] != null)
                        strReturn = _nodWeaponBonus["apreplace"].InnerText;
                    // Use the ap bonus if available, otherwise use 0.
                    else if (_nodWeaponBonus["ap"] != null)
                    {
                        strReturn = _nodWeaponBonus["ap"].InnerText;

                        // If this does not start with "-", add a "+" to the string.
                        if (!strReturn.StartsWith("-"))
                            strReturn = "+" + strReturn;
                    }

                    return strReturn;
                }
            }
        }

        /// <summary>
        /// Weapon Bonus Range.
        /// </summary>
        public int WeaponBonusRange
        {
            get
            {
                if (_nodWeaponBonus?["rangebonus"] != null)
                    return Convert.ToInt32(_nodWeaponBonus["rangebonus"].InnerText);
                else
                    return 0;
            }
        }


        /// <summary>
        /// Base Matrix Boxes.
        /// </summary>
        public int BaseMatrixBoxes
        {
            get
            {
                return 8;
            }
        }

        /// <summary>
        /// Bonus Matrix Boxes.
        /// </summary>
        public int BonusMatrixBoxes
        {
            get
            {
                return _intMatrixCMBonus;
            }
            set
            {
                _intMatrixCMBonus = value;
            }
        }

        /// <summary>
        /// Total Bonus Matrix Boxes (including all children).
        /// </summary>
        public int TotalBonusMatrixBoxes
        {
            get
            {
                int intReturn = BonusMatrixBoxes;
                foreach (Gear objGear in Children)
                {
                    if (objGear.Equipped)
                    {
                        intReturn += objGear.TotalBonusMatrixBoxes;
                    }
                }
                return intReturn;
            }
        }

        /// <summary>
        /// Matrix Condition Monitor boxes.
        /// </summary>
        public int MatrixCM
        {
            get
            {
                return BaseMatrixBoxes + (TotalDeviceRating + 1) / 2 + TotalBonusMatrixBoxes;
            }
        }

        /// <summary>
        /// Matrix Condition Monitor boxes filled.
        /// </summary>
        public int MatrixCMFilled
        {
            get
            {
                return _intMatrixCMFilled;
            }
            set
            {
                _intMatrixCMFilled = value;
            }
        }
        #endregion

        #region Methods

        public IEnumerable<Gear> ThisAndAllChildren()
        {
            yield return this;

            foreach (Gear objChild in _objChildren)
            {
                foreach (Gear child in objChild.ThisAndAllChildren())
                {
                    yield return child;
                }
            }
        }

        #endregion
    }
}
