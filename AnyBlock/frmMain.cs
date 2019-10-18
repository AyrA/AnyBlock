using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using static AnyBlock.Logger;
using WinAPI.NET;

namespace AnyBlock
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// All cache data
        /// </summary>
        private JObject AllData
        {
            get
            {
                return (JObject)JsonConvert.DeserializeObject(File.ReadAllText(Cache.CacheFile));
            }
        }

        /// <summary>
        /// Current filter entries
        /// </summary>
        private List<RangeEntry> CurrentRanges;

        //Form initializer
        public frmMain()
        {
            InitializeComponent();
            //Ranges msut be loaded before the filter list
            CurrentRanges = Cache.SelectedRanges.ToList();
            FillList(AllData, null);
            //Selected rules are displayed last
            DisplayRules();
        }

        /// <summary>
        /// Fills the tree view with the base nodes
        /// </summary>
        /// <param name="Data">Raw cache data</param>
        /// <param name="Filter">Optional filter</param>
        /// <returns><see cref="true"/>, if at least one node was added</returns>
        private bool FillList(JObject Data, string Filter)
        {
            var added = false;
            Debug("Filling Tree List view Nodes");

            tvRanges.SuspendLayout();
            tvRanges.AfterCheck -= tvRanges_AfterCheck;
            tvRanges.Nodes.Clear();

            foreach (var Prop in Data.Properties().OrderBy(m => m.Name))
            {
                var Name = Prop.Name;
                var Node = new TreeNode(Name);
                Node.Name = Name;
                Debug(Name);
                if (SetCategories(Node, Prop, Filter) || string.IsNullOrEmpty(Filter) || Name.ToLower().Contains(Filter.ToLower()))
                {
                    added = true;
                    tvRanges.Nodes.Add(Node);
                    Node.Expand();
                }
            }

            tvRanges.ResumeLayout();
            tvRanges.AfterCheck += tvRanges_AfterCheck;
            CheckRules();

            return added;
        }

        /// <summary>
        /// Fills in the intermediate nodes
        /// </summary>
        /// <param name="Parent">Parent node</param>
        /// <param name="Prop">Current cache section</param>
        /// <param name="Filter">Optional filter</param>
        /// <returns><see cref="true"/>, if at least one node was added</returns>
        private bool SetCategories(TreeNode Parent, JProperty Prop, string Filter)
        {
            var added = false;
            foreach (var Cat in ((JObject)Prop.Value).Properties().OrderBy(m => m.Name))
            {
                var Name = Cat.Name;
                var CatNode = new TreeNode(Name);
                CatNode.Name = Name;
                if (SetEntries(Parent, CatNode, Cat, Filter) || string.IsNullOrEmpty(Filter) || Name.ToLower().Contains(Filter.ToLower()))
                {
                    added = true;
                    Parent.Nodes.Add(CatNode);
                }
            }
            return added;
        }

        /// <summary>
        /// Fills in the final level of nodes
        /// </summary>
        /// <param name="Base">Base node</param>
        /// <param name="Parent">Parent node</param>
        /// <param name="Cat">Current cache section</param>
        /// <param name="Filter">Optional filter</param>
        /// <returns><see cref="true"/>, if at least one node was added</returns>
        private bool SetEntries(TreeNode Base, TreeNode Parent, JProperty Cat, string Filter)
        {
            var added = false;
            foreach (var Prop in ((JObject)Cat.Value).Properties())
            {
                var Name = Prop.Name;
                if (string.IsNullOrEmpty(Filter) || Name.ToLower().Contains(Filter.ToLower()))
                {
                    var NameList = new string[] { Base.Name, Cat.Name, Name };
                    var Node = Parent.Nodes.Add(Name);
                    Node.Tag = Prop.Value;
                    Node.Name = Name;
                    added = true;
                }
            }
            return added;
        }

        /// <summary>
        /// Displays all rules in the rule list
        /// </summary>
        private void DisplayRules()
        {
            lbRules.Items.Clear();
            foreach (var Rule in CurrentRanges)
            {
                lbRules.Items.Add(Rule);
            }
        }

        /// <summary>
        /// Checks all existing rules in the tree list
        /// </summary>
        private void CheckRules()
        {
            tvRanges.AfterCheck -= tvRanges_AfterCheck;
            foreach (var Entry in CurrentRanges)
            {
                CheckNode(Entry.Segments);
            }
            tvRanges.AfterCheck += tvRanges_AfterCheck;
        }

        /// <summary>
        /// Checks the specified node
        /// </summary>
        /// <param name="NodePath">Tree node path</param>
        /// <returns><see cref="true"/>, if node was checked</returns>
        private bool CheckNode(string[] NodePath)
        {
            if (NodePath == null || NodePath.Length == 0)
            {
                return false;
            }
            var BaseNode = tvRanges.Nodes[NodePath[0]];
            for (var i = 1; i < NodePath.Length && BaseNode != null; i++)
            {
                BaseNode = BaseNode.Nodes[NodePath[i]];
            }
            if (BaseNode != null)
            {
                BaseNode.Checked = true;
                UncheckAll(BaseNode.Nodes.OfType<TreeNode>());
            }
            return BaseNode != null;
        }

        /// <summary>
        /// Unchecks all child nodes recursively
        /// </summary>
        /// <param name="Nodes">Node list</param>
        private void UncheckAll(IEnumerable<TreeNode> Nodes)
        {
            foreach (var Node in Nodes)
            {
                Node.Checked = false;
                UncheckAll(Node.Nodes.OfType<TreeNode>());
            }
        }

        /// <summary>
        /// Unchecks all parent nodes recursively
        /// </summary>
        /// <param name="Parent">Parent node</param>
        private void UncheckAll(TreeNode Parent)
        {
            if (Parent != null)
            {
                Parent.Checked = false;
                UncheckAll(Parent.Parent);
            }
        }

        /// <summary>
        /// Converts a single tree node into a tree node path
        /// </summary>
        /// <param name="N">Tree node</param>
        /// <returns>Tree node path</returns>
        private string[] GetTreePath(TreeNode N)
        {
            var L = new List<string>();
            while (N != null)
            {
                L.Add(N.Name);
                N = N.Parent;
            }
            return L.Reverse<string>().ToArray();
        }

        private void tvRanges_AfterCheck(object sender, TreeViewEventArgs e)
        {
            var Master = e.Node;
            var Route = GetTreePath(Master);
            if (Master.Checked)
            {
                UncheckAll(Master.Parent);
                UncheckAll(Master.Nodes.OfType<TreeNode>());
                CurrentRanges.Add(new RangeEntry()
                {
                    Direction = Direction.IN,
                    Segments = Route
                });
            }
            else
            {
                CurrentRanges.RemoveAll(m => m.Segments.SequenceEqual(Route));
            }
            DisplayRules();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var Rules = lbRules.Items.OfType<RangeEntry>().ToArray();
            if (!Rules.All(m => Cache.ValidEntry(m.Segments)))
            {
                MessageBox.Show($"Invalid Rules. This is likely because a Ruleset disappeared.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Cache.SelectedRanges = Rules;
                CurrentRanges = Rules.ToList();
                MessageBox.Show("Changes Saved. Run Application with /apply argument to update Firewall Rules", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lbRules_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbRules.SelectedIndex >= 0)
            {
                var Item = ((RangeEntry)lbRules.SelectedItem).Segments;
                TreeNode Node = tvRanges.Nodes[Item[0]];
                foreach (var E in Item.Skip(1))
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

        private void tbFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = e.Handled = true;
                FillList(AllData, tbFilter.Text);
            }
        }
    }
}
