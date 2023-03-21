using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ESDStudio.Views;

public partial class CreateNewProject : Window
{
    public CreateNewProject()
    {
        InitializeComponent();
        TextBox_ProjDir.Text = AppDomain.CurrentDomain.BaseDirectory + @"Projects\MyProject";
    }

    private void Button_FindGameExe_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fileDialog = new()
        {
            Filter = "Executable File (*.exe)|*.exe"
        };
        if (fileDialog.ShowDialog() == true)
        {
            TextBox_ExePath.Text = fileDialog.FileName;

            if (fileDialog.FileName.Contains("DarkSoulsIII"))
            {
                Label_DetectedGame.Content = "Dark Souls 3";
            }
        }
    }

    private void Button_ProjCreationDone_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(TextBox_ProjDir.Text);
        ESDStudioProject newProject = new(TextBox_ProjName.Text, TextBox_ProjDir.Text, TextBox_ModDir.Text,
            TextBox_ExePath.Text, (string)Label_DetectedGame.Content);
        newProject.WriteToml();
        (Application.Current.MainWindow as MainWindow)?.LoadProject(newProject);
        Close();
    }
}