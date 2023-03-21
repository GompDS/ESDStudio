using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using SoulsFormats;
using Path = System.Windows.Shapes.Path;

namespace ESDStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        public ESDStudioProject? CurrentProject { get; set; }
        public Dictionary<string, BND4> TalkBNDs = new();
        public Dictionary<string, BNDInfo> BNDInfos = new();
        public ESDInfo? CurrentESDInfo;
        public List<CodeEditorTab> CodeEditorTabs = new();
        public CodeEditorTab? ActiveTab { get; set; }

        private void ResetProjectValues()
        {
            CurrentProject = null;
            TalkBNDs = new Dictionary<string, BND4>();
            BNDInfos = new Dictionary<string, BNDInfo>();
            CurrentESDInfo = null;
            CodeEditorTabs = new List<CodeEditorTab>();
            ActiveTab = null;
        }

        public bool AddNewESDToBND(int ESDId, string BNDName)
        {
            BNDInfos.TryGetValue(BNDName, out BNDInfo? bndInfo);
            if (bndInfo != null)
            {
                int mapId = int.Parse(BNDName[1..3]);
                int blockId = int.Parse(BNDName[4..6]);
                string ESDName = $"t{mapId:D2}{blockId:D1}{ESDId:D3}";
                string codeTemplate = $"# -*- coding: utf-8 -*-\r\ndef {ESDName}_1()\r\n    Quit()";
                TreeViewItem? BNDTreeItem = TreeView_ESDTree.Items.Cast<TreeViewItem>()
                    .FirstOrDefault(x => x.Header.Equals(BNDName));
                if (BNDTreeItem != null)
                {
                    BNDTreeItem.IsSelected = true;
                    BNDTreeItem.IsExpanded = true;
                    TreeViewItem newItem = new();
                    newItem.Header = ESDName;
                    for (int i = 0; i < BNDTreeItem.Items.Count; i++)
                    {
                        TreeViewItem ESDTreeItem = (TreeViewItem)BNDTreeItem.Items[i];
                        string header = (string) ESDTreeItem.Header;
                        if (string.Compare(header, ESDName, StringComparison.Ordinal) > 0)
                        {
                            BNDTreeItem.Items.Insert(i, newItem);
                            break;
                        }
                    }
                    ESDInfo newESD = new ESDInfo(codeTemplate, ESDName, bndInfo, newItem);
                    bndInfo.ESDInfos.Add(ESDName, newESD);
                    BNDTreeItem.IsSelected = false;
                    newItem.IsSelected = true;
                    return true;
                }
            }
            return false;
        }

        private void Project_create_OnClick(object sender, RoutedEventArgs e)
        {
            CreateNewProject newProjectWindow = new()
            {
                ShowInTaskbar = false,
                Owner = Application.Current.MainWindow
            };
            newProjectWindow.Show();
        }

        private void MenuItem_OpenProject_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new()
            {
                Filter = "TOML files (*.toml)|*.toml"
            };
            if (fileDialog.ShowDialog() == true)
            {
                LoadProject(new ESDStudioProject(fileDialog.FileName));
            }
        }

        public bool UnloadProject()
        {
            if (BNDInfos.Values.Any(x => x.IsEdited))
            {
                MessageBoxResult result = MessageBox.Show($"The current project has unsaved changes. Would you like to save all changes before closing it?", "Unsaved Changes",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveAll();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            for (int i = 0; i < CodeEditorTabs.Count; i++)
            {
                CloseTab(CodeEditorTabs[i]);
            }

            CurrentProject = null;
            TreeView_ESDTree.Items.Clear();
            ResetProjectValues();
            return true;
        }

        public void LoadProject(ESDStudioProject project)
        {
            if (CurrentProject != null)
            {
                bool shouldContinue = UnloadProject();
                if (shouldContinue == false) return;
            }
            CurrentProject = project;
            Label_ProjectName.Content = CurrentProject.Name;
            List<string> modtalkBNDNames = new();
            IEnumerable<string> bndPaths =
                Directory.EnumerateFiles(CurrentProject.ModDirectory + @"\script\talk", "*.talkesdbnd.dcx");
            foreach (string bndPath in bndPaths)
            {
                string bndName =
                    System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(bndPath));
                modtalkBNDNames.Add(bndName);
                TalkBNDs.Add(bndName, BND4.Read(bndPath));
            }
            
            bndPaths =
                Directory.EnumerateFiles(CurrentProject.GameDirectory + @"\script\talk", "*.talkesdbnd.dcx");
            foreach (string bndPath in bndPaths.Where(x => !modtalkBNDNames.Any(x.Contains)))
            {
                string bndName =
                    System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(bndPath));
                TalkBNDs.Add(bndName, BND4.Read(bndPath));
            }
            
            foreach (string bndName in TalkBNDs.Keys.OrderBy(x => x))
            {
                TreeViewItem newBND = new()
                {
                    Header = bndName
                };
                ContextMenu bndMenu = new ContextMenu();
                MenuItem createESD = new();
                createESD.Header = "Create New ESD";
                createESD.Click += MenuItem_CreateESD_OnClick;
                bndMenu.Items.Add(createESD);
                newBND.ContextMenu = bndMenu;
                BNDInfo newBndInfo = new(bndName, TalkBNDs, newBND);
                BNDInfos.Add(bndName, newBndInfo);
                TreeView_ESDTree.Items.Add(newBND);
                foreach (BinderFile file in TalkBNDs[bndName].Files)
                {
                    TreeViewItem newESD = new()
                    {
                        Header = System.IO.Path.GetFileNameWithoutExtension(file.Name)
                    };
                    ContextMenu esdMenu = new ContextMenu
                    {
                        Items =
                        {
                            new MenuItem
                            {
                                Header = "Rename"
                            },
                            new MenuItem
                            {
                                Header = "Duplicate"
                            },
                            new MenuItem
                            {
                                Header = "Delete"
                            } 
                        }
                    };
                    newESD.ContextMenu = esdMenu;
                    newBND.Items.Add(newESD);
                }
            }

            Activate();
            TreeView_ESDTree.Visibility = Visibility.Visible;
        }

        private void MenuItem_CreateESD_OnClick(object sender, RoutedEventArgs e)
        {
            CreateNewESD dialog = new CreateNewESD
            {
                ShowInTaskbar = false,
                Owner = Application.Current.MainWindow
            };
            
            foreach (string BNDId in BNDInfos.Keys)
            {
                ComboBoxItem newComboItem = new()
                {
                    Content = BNDId
                };
                dialog.BNDChoice.Items.Add(newComboItem);
            }

            if (((ContextMenu)((MenuItem)sender).Parent).PlacementTarget is TreeViewItem parentTreeItem)
            {
                string parentBNDId = ((string)parentTreeItem.Header)[..12];
                ComboBoxItem? matchingItem = dialog.BNDChoice.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(x => x.Content.Equals(parentBNDId));
                if (matchingItem != null)
                {
                    matchingItem.IsSelected = true;
                }
            }
            
            bool? result = dialog.ShowDialog();
        }

        private void TreeView_ESDTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (CurrentProject == null || TreeView_ESDTree.SelectedItem == null) return;
            TreeViewItem selectedTreeItem = (TreeViewItem)TreeView_ESDTree.SelectedItem;
            string itemHeader = ((string)selectedTreeItem.Header)[..7];
            if (!Regex.IsMatch(itemHeader, "t\\d\\d\\d\\d\\d\\d")) return;
            string parentHeader = ((string)((TreeViewItem)selectedTreeItem.Parent).Header)[..12];
            BNDInfo parentInfo = BNDInfos[parentHeader];
            parentInfo.ESDInfos.TryGetValue(itemHeader, out CurrentESDInfo);
            if (CurrentESDInfo == null)
            {
                if (File.Exists(CurrentProject.BaseDirectory + $"\\{parentHeader}\\{itemHeader}.py"))
                {
                    CurrentESDInfo = new ESDInfo(File.ReadAllText(CurrentProject.BaseDirectory + $"\\{parentHeader}\\{itemHeader}.py"), itemHeader, parentInfo, selectedTreeItem);
                }
                else
                {
                    BinderFile esdFile = parentInfo.BND.Files.First(x => x.Name.Contains(itemHeader));
                    string cwd = AppDomain.CurrentDomain.BaseDirectory + @"\esdtool";
                    File.WriteAllBytes($"{cwd}\\{itemHeader}.esd", esdFile.Bytes);
                    Process esdtool = new()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = $"{cwd}\\esdtool.exe",
                            WorkingDirectory = cwd,
                            Arguments = $"-i {itemHeader}.esd -writepy %e.py"
                        }
                    };
                    esdtool.Start();
                    esdtool.WaitForExit();
                    CurrentESDInfo = new ESDInfo(File.ReadAllText($"{cwd}\\{itemHeader}.py"), itemHeader, parentInfo, selectedTreeItem);
                    File.Delete($"{cwd}\\{itemHeader}.esd");
                    File.Delete($"{cwd}\\{itemHeader}.py");
                    parentInfo.ESDInfos.Add(itemHeader, CurrentESDInfo);
                }
            }
            
            OpenNewTab();
        }

        private void Save(ESDInfo? esdToSave)
        {
            if (CurrentProject == null || esdToSave == null) return;
            string projectBNDFolder = CurrentProject.BaseDirectory + $"\\{esdToSave.ParentBND.Id}";
            if (Directory.Exists(projectBNDFolder) == false)
            {
                Directory.CreateDirectory(projectBNDFolder);
            }
            File.WriteAllText(projectBNDFolder + $"\\{esdToSave.Id}.py", esdToSave.Code);
            esdToSave.IsEdited = false;
            esdToSave.TreeItem.Header = esdToSave.Id;
            if (esdToSave.ParentBND.ESDInfos.All(x => x.Value.IsEdited == false))
            {
                ((TreeViewItem)esdToSave.TreeItem.Parent).Header = esdToSave.ParentBND.Id;
                esdToSave.ParentBND.IsEdited = false;
            }

            CodeEditorTab? codeTab = CodeEditorTabs.FirstOrDefault(x => x.ESD == esdToSave);
            if (codeTab != null)
            {
                codeTab.EnterButton.Content = esdToSave.Id;
            }
        }

        private void SaveAll()
        {
            foreach (string bndId in BNDInfos.Keys.Where(x => BNDInfos[x].IsEdited))
            {
                BNDInfo bndInfo = BNDInfos[bndId];
                foreach (string esdId in bndInfo.ESDInfos.Keys.Where(x => bndInfo.ESDInfos[x].IsEdited))
                {
                    ESDInfo esdInfo = bndInfo.ESDInfos[esdId];
                    Save(esdInfo);
                }
            }
        }

        private void CommandBinding_Save_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Save(CurrentESDInfo);
        }
        
        private void CommandBinding_SaveAll_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SaveAll();
        }

        private void OpenNewTab()
        {
            if (CurrentESDInfo == null) return;
            CodeEditorTab? codeTab = CodeEditorTabs.FirstOrDefault(x => x.ESD == CurrentESDInfo);
            if (codeTab != null)
            {
                ActiveTab?.Hide();
                ActiveTab = codeTab;
                ActiveTab.Show();
            }
            else
            {
                CodeEditorTab newTab = new(CurrentESDInfo);
                newTab.EnterButton.Click += ClickOnTab;
                newTab.TextEditor.TextChanged += MarkFileAsChanged;
                StackPanel_EditorTabs.Children.Add(newTab.EnterButton);
                EditorGrid.Children.Add(newTab.TextEditor);
                CodeEditorTabs.Add(newTab);
                ActiveTab?.Hide();
                ActiveTab = newTab;
                ActiveTab.Show();
            }
        }

        private void ClickOnTab(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button) sender;
            CodeEditorTab? chosenTab = CodeEditorTabs.FirstOrDefault(x => x.EnterButton == clickedButton);
            if (chosenTab != null)
            {
                ActiveTab?.Hide();
                ActiveTab = chosenTab;
                ActiveTab.Show();
            }
        }
        
        private void MarkFileAsChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentESDInfo == null) return;
            if (CurrentESDInfo.ParentBND.IsEdited == false)
            {
                ((TreeViewItem)CurrentESDInfo.TreeItem.Parent).Header = CurrentESDInfo.ParentBND.Id.Insert(12, "*");
                CurrentESDInfo.ParentBND.IsEdited = true;
            }
            if (CurrentESDInfo.IsEdited == false)
            {
                CurrentESDInfo.IsEdited = true;
                CurrentESDInfo.TreeItem.Header = CurrentESDInfo.Id.Insert(7, "*");
                if (ActiveTab != null) ActiveTab.EnterButton.Content = CurrentESDInfo.Id.Insert(7, "*");
            }
            CurrentESDInfo.Code = ((TextBox)sender).Text;
        }

        private void CommandBinding_CloseTab_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ActiveTab == null) return;
            if (ActiveTab.ESD.IsEdited)
            {
                MessageBoxResult result = MessageBox.Show($"{ActiveTab.ESD.Id} has unsaved changes. Would you like to save changes before closing this tab?", "Unsaved Changes",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Save(ActiveTab.ESD);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            CloseTab(ActiveTab);
        }

        private void CloseTab(CodeEditorTab tab)
        {
            int tabIndex = CodeEditorTabs.IndexOf(tab);
            StackPanel_EditorTabs.Children.Remove(tab.EnterButton);
            EditorGrid.Children.Remove(tab.TextEditor);
            CodeEditorTabs.Remove(tab);
            if (CodeEditorTabs.Count > tabIndex)
            {
                ActiveTab = CodeEditorTabs[tabIndex];
            }
            else if (tabIndex > 0)
            {
                ActiveTab = CodeEditorTabs[tabIndex - 1];
            }
            else
            {
                ActiveTab = null;
                tab.ESD.TreeItem.IsSelected = false;
                return;
            }
            ActiveTab.Show();   
            ActiveTab.ESD.TreeItem.IsSelected = true;
        }
    }
}