﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tpv.Ui.Model;
using Tpv.Ui.Service;

namespace Tpv.Ui.View
{
    /// <summary>
    /// Interaction logic for PromoModWnd.xaml
    /// </summary>
    public partial class PromoModWnd
    {

        public PromoModWnd()
        {
            InitializeComponent();
        }

        public int CodeGroupModifier { get; set; }
        public string NameGroupModifier { get; set; }
        public List<ItemGroupModifier> LstModifierGroup { get; set; }

        private StackPanel _selectedItemTicket;

        private ItemGroupModifier _selectedGrpModifer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitModifierGroup();
        }

        private void InitModifierGroup()
        {
            TxtTitle.Text = "Modify " + NameGroupModifier;
            SetItemList(new ItemTicket
            {
                Name = NameGroupModifier,
                IsMain = true,
                PriceVal = 0,
            });
            ReadFromAloha();
        }

        private void SetItemList(ItemTicket ticket)
        {
            var txtItem = new TextBlock
            {
                Text = ticket.Name,
                //Foreground = new SolidColorBrush(Color.FromRgb(51, 204, 255)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 1, 0),
                FontFamily = new FontFamily("Batang"),
                FontSize = 16,
                Width = 220
            };

            var txtPrice = new TextBlock
            {
                Text = ticket.PriceTxt,
                //Foreground = new SolidColorBrush(Color.FromRgb(51, 204, 255)),
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(1, 0, 0, 0),
                FontFamily = new FontFamily("Batang"),
                FontSize = 16,
                Width = 50
            };

            var stPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                //Background = new SolidColorBrush(Colors.Black),
                Tag = ticket
            };

            stPanel.MouseUp += StPanelOnMouseUp;

            stPanel.Children.Add(txtItem);
            stPanel.Children.Add(txtPrice);

            StPnTicket.Children.Add(stPanel);

        }

        private void StPanelOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var stkPanel = sender as StackPanel;

            if (stkPanel == null)
                return;

            var item = stkPanel.Tag as ItemTicket;

            if (item == null)
                return;

            ClearAllItems();
            _selectedItemTicket = null;

            if (item.IsMain)
                return;

            _selectedItemTicket = stkPanel;
            stkPanel.Background = new SolidColorBrush(Colors.Black);
            foreach (var childInt in stkPanel.Children)
            {
                var txtChild = childInt as TextBlock;

                if (txtChild == null)
                    continue;

                txtChild.Foreground = new SolidColorBrush(Color.FromRgb(51, 204, 255));
            }
        }

        private void ClearAllItems()
        {
            foreach (var child in StPnTicket.Children)
            {
                var stChild = child as StackPanel;

                if (stChild == null)
                    continue;

                stChild.Background = null;
                foreach (var childInt in stChild.Children)
                {
                    var txtChild = childInt as TextBlock;

                    if(txtChild == null)
                        continue;

                    txtChild.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void ReadFromAloha()
        {
            try
            {
                LstModifierGroup = new List<ItemGroupModifier>();

                LasaFOHLib67.IberDepot depot;
                LasaFOHLib67.IIberObject parent;

                try
                {
                    depot = new LasaFOHLib67.IberDepotClass();
                    parent = depot.FindObjectFromId(740, CodeGroupModifier).First();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Aloha no se está ejecutando o no existe un ticket activo.", "TPV error",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Close();
                    return;
                }

                var modgrpIds = new List<int>();
                for (var x = 1; x <= 10; x++)
                {
                    var id = parent.GetLongVal(string.Format("MOD{0}", x));
                    if (id > 0)
                        modgrpIds.Add(id);
                }

                try
                {
                    LasaFOHLib67.IIberEnum modgroups = depot.GetEnum(16);
                    LasaFOHLib67.IIberObject modgroup = modgroups.First();
                    while (modgroup != null)
                    {
                        var mgid = modgroup.GetLongVal("ID");
                        if (modgrpIds.Contains(mgid))
                        {
                            LstModifierGroup.Add(new ItemGroupModifier
                            {
                                Name = modgroup.GetStringVal("LONGNAME"),
                                Minimum = modgroup.GetStringVal("MINIMUM"),
                                Maximum = modgroup.GetStringVal("MAXIMUM"),
                                Id = mgid
                            });
                        }
                        modgroup = modgroups.Next();
                    }
                }
                catch (Exception)
                {
                    // expected
                }

            }
            catch (Exception)
            {
                // expected
            }


            if (LstModifierGroup.Count == 0)
                return;

            LstModifierGroup = LstModifierGroup.OrderBy(e => e.Name).ToList();
            
            var i = 0;
            foreach (var groupModifier in LstModifierGroup)
            {
                var button = FactoryButton.CreateModifierGroupButton(groupModifier, i++, OnClickGroupModifier);
                StGroupMod.Children.Add(button);

                if (i == 1) //El primero debe de ir por sus modificadores
                    FillModifierSection(groupModifier);

            }
        }

        public void OnClickGroupModifier(object o, RoutedEventArgs routedEventArgs)
        {
            var button = o as Button;

            if(button == null)
                return;

            var item = button.Tag as ItemGroupModifier;
            
            if(item != null)
                FillModifierSection(item);
        }


        private void FillModifierSection(ItemGroupModifier groupModifier)
        {
            WrModifier.Children.Clear();

            try
            {
                LasaFOHLib67.IberDepot depot = new LasaFOHLib67.IberDepotClass();
                LasaFOHLib67.IIberObject modgroup = null;
                // get the groupModifier group
                try
                {
                    LasaFOHLib67.IIberEnum modgroups = depot.GetEnum(16);
                    modgroup = modgroups.First();
                    while (modgroup != null)
                    {
                        if (modgroup.GetLongVal("ID") == groupModifier.Id)
                            break;

                        modgroup = modgroups.Next();
                    }
                }
                catch (Exception)
                {
                    // expected
                }

                if (modgroup == null)
                {
                    // sanity check.
                    // we shouldn't have gotten here if the groupModifier group doesn't exist
                    return;
                }

                var listModifiers = new List<ItemModifier>();
                // there can be up to 54 modifiers in a group
                for (int x = 1; x <= 54; x++)
                {
                    int itemId = modgroup.GetLongVal(String.Format("ITEM{0:D2}", x));
                    if (itemId > 0)
                    {
                        // find the item
                        try
                        {
                            LasaFOHLib67.IIberObject item = depot.FindObjectFromId(740, itemId).First();
                            listModifiers.Add(new ItemModifier
                            {
                                Id = itemId,
                                Name = item.GetStringVal("LONGNAME"),
                                Price = item.GetStringVal("PRICE"),
                                GroupModifier = groupModifier
                            });
                        }
                        catch (Exception)
                        {
                            // expected
                        }
                    }
                }

                listModifiers = listModifiers.OrderBy(e => e.Name).ToList();
                foreach (var modifier in listModifiers)
                {
                    var button = FactoryButton.CreateModifierButton(modifier, OnClickModifier);
                    WrModifier.Children.Add(button);
                }

                _selectedGrpModifer = groupModifier;
            }
            catch (Exception)
            {
                // expected
            }
        }

        public void OnClickModifier(object o, RoutedEventArgs routedEventArgs)
        {
            var button = o as Button;

            if (button == null)
                return;

            var item = button.Tag as ItemModifier;

            if(item == null)
                return;

            SetItemList(new ItemTicket
            {
                Item = item,
                Name = item.Name,
                PriceVal = item.PriceVal,
                IsMain = false
            });
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItemTicket == null)
            {
                ShowMessage("Select the modifier to delete by touching the Work Area above this button.");
                return;
            }

            StPnTicket.Children.Remove(_selectedItemTicket);
            _selectedItemTicket = null;
        }

        private void ShowMessage(string msgTx)
        {
            var msg = new MsgWnd(msgTx)
            {
                Owner = this
            };
            msg.ShowDialog();
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            ScVwMod.PageDown();
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            ScVwMod.PageUp();
        }

        private void ScVwMod_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            BtnDown.Visibility = Visibility.Hidden;
            BtnUp.Visibility = Visibility.Hidden;

            if (ScVwMod.ExtentHeight > ScVwMod.ViewportHeight && ScVwMod.ContentVerticalOffset + ScVwMod.ViewportHeight < ScVwMod.ExtentHeight)
                BtnDown.Visibility = Visibility.Visible;

            if(ScVwMod.ContentVerticalOffset > 0)
                BtnUp.Visibility = Visibility.Visible;
        }

        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            for (var i = StPnTicket.Children.Count-1; i >= 0; i--)
            {
                var item = StPnTicket.Children[i] as StackPanel;
                
                if(item == null)
                    continue;

                var itemTag = item.Tag as ItemTicket;

                if (itemTag == null || itemTag.Item == null)
                    continue;

                if (itemTag.Item.GroupModifier == _selectedGrpModifer)
                {
                    if (Equals(_selectedItemTicket, item))
                        _selectedItemTicket = null;

                    StPnTicket.Children.RemoveAt(i);
                }
            }
        }
    }
}
