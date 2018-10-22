using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using static AnyBlock.Logger;

namespace AnyBlock
{
    public partial class frmMain : Form
    {
        private bool Suspended = false;
        private RangeEntry[] CurrentRanges;

        public frmMain()
        {
            InitializeComponent();
            var Data = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(Cache.CacheFile));
            FillList(Data);
            CurrentRanges = Cache.SelectedRanges;
            DisplayRules(tvRanges.Nodes.OfType<TreeNode>());
        }

        private void FillList(JObject Data)
        {
            Debug("Filling Tree List view Nodes");
            Suspended = true;
            tvRanges.Nodes.Clear();
            var Ranges = Cache.SelectedRanges;
            foreach (var Prop in Data.Properties())
            {
                var Node = tvRanges.Nodes.Add(Prop.Name);
                Node.Name = Prop.Name;
                Node.Checked = Ranges.Any(m => m.Name == Prop.Name);
                Debug(Node.FullPath);
                SetCategories(Node, Prop, Ranges);
                Node.Expand();
            }
            Suspended = false;
        }

        private void SetCategories(TreeNode Node, JProperty Prop, RangeEntry[] Selected)
        {
            foreach (var Cat in ((JObject)Prop.Value).Properties())
            {
                var CatNode = Node.Nodes.Add(Cat.Name);
                CatNode.Name = Cat.Name;
                Debug(CatNode.FullPath);
                CatNode.Checked = Selected.Any(m => m.Name == $"{Node.Name}.{Cat.Name}");
                SetEntries(CatNode, Cat, Selected);
            }
        }

        private void SetEntries(TreeNode CatNode, JProperty Cat, RangeEntry[] Selected)
        {
            foreach (var Prop in ((JObject)Cat.Value).Properties())
            {
                var Node = CatNode.Nodes.Add(Prop.Name);
                Node.Tag = Prop.Value;
                Node.Name = Prop.Name;
                Debug(Node.FullPath);
                Node.Checked = Selected.Any(m => m.Name == $"{CatNode.Parent.Name}.{Cat.Name}.{Node.Name}");
            }
        }

        private void DisplayRules(IEnumerable<TreeNode> Nodes)
        {
            foreach (var N in Nodes)
            {
                if (N.Checked)
                {
                    var FullName = N.FullPath.Replace('\\', '.');
                    Debug($"Adding Rule: {FullName}");
                    if (CurrentRanges.Any(m => m.Name == FullName))
                    {
                        lbRules.Items.Add(CurrentRanges.First(m => m.Name == FullName));
                    }
                    else
                    {
                        lbRules.Items.Add(new RangeEntry() { Name = FullName, Direction = Direction.IN });
                    }
                }
                else
                {
                    DisplayRules(N.Nodes.OfType<TreeNode>());
                }
            }
        }

        private void tvRanges_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!Suspended)
            {
                var Master = e.Node;
                if (Master.Checked)
                {
                    foreach (var SubNode in Master.Nodes.OfType<TreeNode>())
                    {
                        SubNode.Checked = false;
                    }
                }
                CurrentRanges = lbRules.Items.OfType<RangeEntry>().ToArray();
                lbRules.Items.Clear();
                DisplayRules(tvRanges.Nodes.OfType<TreeNode>());
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var Rules = lbRules.Items.OfType<RangeEntry>().ToArray();
            if (!Rules.All(m => Cache.ValidEntry(m.Name)))
            {
                MessageBox.Show($"Invalid Rules. This is likely because a Ruleset disappeared.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                CurrentRanges = Cache.SelectedRanges = Rules;
                MessageBox.Show("Changes Saved. Run Application with /apply argument to update Firewall Rules", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lbRules_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbRules.SelectedIndex >= 0)
            {
                var Item = ((RangeEntry)lbRules.SelectedItem).Name;
                TreeNode Node = tvRanges.Nodes[Item.Split('.').First()];
                foreach (var E in Item.Split('.').Skip(1))
                {
                    if (Node != null)
                    {
                        Node = Node.Nodes[E];
                    }
                }
                if (Node != null)
                {
                    tvRanges.SelectedNode = Node;
                }
            }
        }

        private void lbRules_DoubleClick(object sender, EventArgs e)
        {
            if (lbRules.SelectedIndex >= 0)
            {
                var Item = (RangeEntry)lbRules.SelectedItem;
                var Index = lbRules.SelectedIndex;
                ++Item.Direction;
                if (!Enum.IsDefined(typeof(Direction), Item.Direction))
                {
                    Item.Direction = Direction.DISABLED;
                }
                lbRules.Items.RemoveAt(Index);
                lbRules.Items.Insert(Index, Item);
            }
        }
    }
}
