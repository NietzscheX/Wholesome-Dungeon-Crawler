﻿using System;
using System.Windows;
using System.Windows.Controls;
using WholesomeDungeonCrawler.CrawlerSettings;
using WholesomeDungeonCrawler.Helpers;

namespace WholesomeDungeonCrawler.GUI
{
    public partial class ProductSettingsControl : UserControl
    {
        public ProductSettingsControl()
        {
            InitializeComponent();
            DataContext = WholesomeDungeonCrawlerSettings.CurrentSetting;
            cbLFGRole.ItemsSource = Enum.GetValues(typeof(LFGRoles));
        }

        private void btnProfileEditor_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                ProfileEditor profileEditor = new ProfileEditor();
                profileEditor.ShowDialog();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }

        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            WholesomeDungeonCrawlerSettings.CurrentSetting.Save();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdvancedSettings advancedSettings = new AdvancedSettings();
                advancedSettings.ShowDialog();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        private void cbLFGRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WholesomeDungeonCrawlerSettings.CurrentSetting.Save();
        }
    }
}
