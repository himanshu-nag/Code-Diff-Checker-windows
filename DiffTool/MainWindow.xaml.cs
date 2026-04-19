using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Microsoft.Win32;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace DiffTool;

public class DiffLine
{
    public string LeftText { get; set; } = string.Empty;
    public string RightText { get; set; } = string.Empty;
    public System.Windows.Media.Brush LeftBackground { get; set; } = System.Windows.Media.Brushes.Transparent;
    public System.Windows.Media.Brush RightBackground { get; set; } = System.Windows.Media.Brushes.Transparent;
    public System.Windows.Media.Brush LeftForeground { get; set; } = System.Windows.Media.Brushes.Black;
    public System.Windows.Media.Brush RightForeground { get; set; } = System.Windows.Media.Brushes.Black;
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly List<DiffLine> _diffLines = new();

    public MainWindow()
    {
        InitializeComponent();
        UpdateViewMode();
    }

    private void btnSelect1_Click(object sender, RoutedEventArgs e)
    {
        if (rbFiles.IsChecked == true)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtPath1.Text = openFileDialog.FileName;
                TryRealTimeCompare();
            }
        }
        else
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath1.Text = dialog.SelectedPath;
                TryRealTimeCompare();
            }
        }
    }

    private void btnSelect2_Click(object sender, RoutedEventArgs e)
    {
        if (rbFiles.IsChecked == true)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtPath2.Text = openFileDialog.FileName;
                TryRealTimeCompare();
            }
        }
        else
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath2.Text = dialog.SelectedPath;
                TryRealTimeCompare();
            }
        }
    }

    private void btnCompare_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCompare();
    }

    private void ExecuteCompare()
    {
        if (string.IsNullOrEmpty(txtPath1.Text) || string.IsNullOrEmpty(txtPath2.Text))
        {
            System.Windows.MessageBox.Show("Please select both paths.");
            return;
        }

        if (rbFiles.IsChecked == true)
        {
            CompareFiles(txtPath1.Text, txtPath2.Text);
        }
        else
        {
            CompareFolders(txtPath1.Text, txtPath2.Text);
        }
    }

    private void CompareFiles(string file1, string file2)
    {
        if (!File.Exists(file1) || !File.Exists(file2))
        {
            ShowMessageInUnified("One or both files do not exist.");
            return;
        }

        var text1 = File.ReadAllText(file1);
        var text2 = File.ReadAllText(file2);

        // Display file info
        var fi1 = new FileInfo(file1);
        var fi2 = new FileInfo(file2);
        lblLeftFileInfo.Text = $"{Path.GetFileName(file1)} • {fi1.Length} bytes";
        lblRightFileInfo.Text = $"{Path.GetFileName(file2)} • {fi2.Length} bytes";

        if (chkUnifiedDiff.IsChecked == true)
        {
            ShowUnifiedDiff(text1, text2);
            return;
        }

        var differ = new Differ();
        var builder = new SideBySideDiffBuilder(differ);
        var result = builder.BuildDiffModel(text1, text2);

        var leftDoc = new FlowDocument();
        var rightDoc = new FlowDocument();
        var leftLineNumbers = new FlowDocument();
        var rightLineNumbers = new FlowDocument();
        
        var leftLines = result.OldText.Lines;
        var rightLines = result.NewText.Lines;
        var max = Math.Max(leftLines.Count, rightLines.Count);
        int additions = 0;
        int removals = 0;
        int changedLines = 0;

        for (int i = 0; i < max; i++)
        {
            var left = i < leftLines.Count ? leftLines[i] : null;
            var right = i < rightLines.Count ? rightLines[i] : null;

            // Left line number
            if (i < leftLines.Count)
            {
                var lineNumPara = new Paragraph(new Run((i + 1).ToString())) { Margin = new Thickness(0, 2, 0, 2) };
                leftLineNumbers.Blocks.Add(lineNumPara);
            }

            // Right line number
            if (i < rightLines.Count)
            {
                var lineNumPara = new Paragraph(new Run((i + 1).ToString())) { Margin = new Thickness(0, 2, 0, 2) };
                rightLineNumbers.Blocks.Add(lineNumPara);
            }

            var leftPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            var rightPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };

            if (left != null)
            {
                var leftRun = new Run(left.Text ?? string.Empty);
                if (left.Type == ChangeType.Deleted || left.Type == ChangeType.Modified)
                {
                    leftRun.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 100, 100));
                    leftRun.Foreground = Brushes.Black;
                    removals++;
                    changedLines++;
                }
                leftPara.Inlines.Add(leftRun);
            }
            leftDoc.Blocks.Add(leftPara);

            if (right != null)
            {
                var rightRun = new Run(right.Text ?? string.Empty);
                if (right.Type == ChangeType.Inserted || right.Type == ChangeType.Modified)
                {
                    rightRun.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 100, 255, 100));
                    rightRun.Foreground = Brushes.Black;
                    additions++;
                    changedLines++;
                }
                rightPara.Inlines.Add(rightRun);
            }
            rightDoc.Blocks.Add(rightPara);
        }

        rtbLeft.Document = leftDoc;
        rtbRight.Document = rightDoc;
        rtbLeftLineNumbers.Document = leftLineNumbers;
        rtbRightLineNumbers.Document = rightLineNumbers;
        
        UpdateSummary(additions, removals);
        UpdateStatistics(changedLines, additions, removals, max);
        ShowSideBySide();
        statusText.Text = "Side-by-side diff loaded. Select and copy any part.";
    }

    private void ShowUnifiedDiff(string oldText, string newText)
    {
        var differ = new Differ();
        var inlineBuilder = new InlineDiffBuilder(differ);
        var result = inlineBuilder.BuildDiffModel(oldText, newText);
        var document = new FlowDocument();
        var lineNumbersDoc = new FlowDocument();

        int additions = 0;
        int removals = 0;
        int lineCount = 0;

        foreach (var line in result.Lines)
        {
            lineCount++;
            var paragraph = new Paragraph { Margin = new Thickness(0) };
            var run = new Run(line.Text);
            
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    run.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 248, 198));
                    additions++;
                    break;
                case ChangeType.Deleted:
                    run.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 230, 230));
                    removals++;
                    break;
                case ChangeType.Modified:
                    run.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 244, 179));
                    additions++;
                    removals++;
                    break;
                default:
                    break;
            }

            paragraph.Inlines.Add(run);
            document.Blocks.Add(paragraph);
            
            // Add line number
            var lineNumPara = new Paragraph(new Run(lineCount.ToString())) { Margin = new Thickness(0) };
            lineNumbersDoc.Blocks.Add(lineNumPara);
        }

        rtbUnified.Document = document;
        rtbLeftLineNumbers.Document = lineNumbersDoc;
        ShowUnified();
        UpdateStatistics(additions + removals, additions, removals, lineCount);
        statusText.Text = "Unified diff loaded. Select and copy any part.";
    }

    private void ShowMessageInUnified(string message)
    {
        var document = new FlowDocument(new Paragraph(new Run(message)));
        rtbUnified.Document = document;
        rtbLeftLineNumbers.Document = new FlowDocument(new Paragraph(new Run("1")));
        ShowUnified();
        UpdateStatistics(0, 0, 0, 0);
        statusText.Text = "Ready";
    }

    private void rbFiles_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void rbFolders_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void MenuSaveDiff_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
        saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
        if (saveFileDialog.ShowDialog() == true)
        {
            string text = string.Empty;
            if (rtbUnified.Visibility == Visibility.Visible)
            {
                text = new TextRange(rtbUnified.Document.ContentStart, rtbUnified.Document.ContentEnd).Text;
            }
            else
            {
                var leftText = new TextRange(rtbLeft.Document.ContentStart, rtbLeft.Document.ContentEnd).Text;
                var rightText = new TextRange(rtbRight.Document.ContentStart, rtbRight.Document.ContentEnd).Text;
                text = $"ORIGINAL:\n{leftText}\n\nCHANGED:\n{rightText}";
            }

            File.WriteAllText(saveFileDialog.FileName, text);
            statusText.Text = "Diff saved.";
        }
    }

    private void MenuCopyDiff_Click(object sender, RoutedEventArgs e)
    {
        string text = string.Empty;
        if (rtbUnified.Visibility == Visibility.Visible)
        {
            text = new TextRange(rtbUnified.Document.ContentStart, rtbUnified.Document.ContentEnd).Text;
        }
        else
        {
            var leftText = new TextRange(rtbLeft.Document.ContentStart, rtbLeft.Document.ContentEnd).Text;
            var rightText = new TextRange(rtbRight.Document.ContentStart, rtbRight.Document.ContentEnd).Text;
            text = $"ORIGINAL:\n{leftText}\n\nCHANGED:\n{rightText}";
        }

        System.Windows.Clipboard.SetText(text);
        statusText.Text = "Diff copied to clipboard.";
    }

    private void MenuClear_Click(object sender, RoutedEventArgs e)
    {
        _diffLines.Clear();
        rtbLeft.Document = new FlowDocument();
        rtbRight.Document = new FlowDocument();
        rtbUnified.Document = new FlowDocument();
        rtbLeftLineNumbers.Document = new FlowDocument();
        rtbRightLineNumbers.Document = new FlowDocument();
        txtPath1.Text = string.Empty;
        txtPath2.Text = string.Empty;
        lblLeftFileInfo.Text = string.Empty;
        lblRightFileInfo.Text = string.Empty;
        lblFileInfo.Text = "Ready to compare";
        UpdateSummary(0, 0);
        UpdateStatistics(0, 0, 0, 0);
        statusText.Text = "Cleared.";
    }

    private void CompareFolders(string folder1, string folder2)
    {
        if (!Directory.Exists(folder1) || !Directory.Exists(folder2))
        {
            ShowMessageInUnified("One or both folders do not exist.");
            return;
        }

        var files1 = Directory.GetFiles(folder1, "*", SearchOption.AllDirectories)
            .Select(f => f.Substring(folder1.Length + 1)).ToHashSet();
        var files2 = Directory.GetFiles(folder2, "*", SearchOption.AllDirectories)
            .Select(f => f.Substring(folder2.Length + 1)).ToHashSet();

        var added = files2.Except(files1).ToList();
        var removed = files1.Except(files2).ToList();
        var common = files1.Intersect(files2).ToList();

        var leftDoc = new FlowDocument();
        var rightDoc = new FlowDocument();
        int additions = 0;
        int removals = 0;

        // Removed files
        foreach (var file in removed)
        {
            var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            var run = new Run($"Removed: {file}");
            run.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 100, 100));
            para.Inlines.Add(run);
            leftDoc.Blocks.Add(para);
            removals++;
        }

        // Added files
        foreach (var file in added)
        {
            var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            var run = new Run($"Added: {file}");
            run.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 100, 255, 100));
            para.Inlines.Add(run);
            rightDoc.Blocks.Add(para);
            additions++;
        }

        // Modified files
        foreach (var file in common)
        {
            var path1 = Path.Combine(folder1, file);
            var path2 = Path.Combine(folder2, file);
            if (!File.ReadAllText(path1).Equals(File.ReadAllText(path2)))
            {
                var leftPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                var leftRun = new Run($"Modified: {file}");
                leftRun.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 200, 100));
                leftPara.Inlines.Add(leftRun);
                leftDoc.Blocks.Add(leftPara);

                var rightPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                var rightRun = new Run($"Modified: {file}");
                rightRun.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 150, 200, 255));
                rightPara.Inlines.Add(rightRun);
                rightDoc.Blocks.Add(rightPara);

                additions++;
                removals++;
            }
        }

        rtbLeft.Document = leftDoc;
        rtbRight.Document = rightDoc;
        UpdateSummary(additions, removals);
        ShowSideBySide();
        statusText.Text = "Folder diff loaded. Select and copy any part.";
    }

    private void UpdateSummary(int additions, int removals)
    {
        lblAdditions.Text = $"+  {additions} additions";
        lblRemovals.Text = $"−  {removals} removals";
    }

    private void UpdateStatistics(int changedLines, int additions, int removals, int totalLines)
    {
        lblChangeStats.Text = changedLines.ToString();
        lblAddStats.Text = additions.ToString();
        lblDelStats.Text = removals.ToString();
        
        if (totalLines > 0)
        {
            double similarity = ((totalLines - changedLines) * 100.0) / totalLines;
            lblSimilarity.Text = $"{similarity:F1}% match";
        }
        else
        {
            lblSimilarity.Text = "0% match";
        }
    }

    private void TryRealTimeCompare()
    {
        if (chkRealTime.IsChecked == true)
        {
            ExecuteCompare();
        }
    }

    private void chkRealTime_Checked(object sender, RoutedEventArgs e)
    {
        TryRealTimeCompare();
    }

    private void chkUnifiedDiff_Checked(object sender, RoutedEventArgs e)
    {
        UpdateViewMode();
        if (!string.IsNullOrEmpty(txtPath1.Text) && !string.IsNullOrEmpty(txtPath2.Text))
        {
            ExecuteCompare();
        }
    }

    private void UpdateViewMode()
    {
        if (chkUnifiedDiff.IsChecked == true)
        {
            ShowUnified();
        }
        else
        {
            ShowSideBySide();
        }
    }

    private void ShowSideBySide()
    {
        rtbLeft.Visibility = Visibility.Visible;
        rtbRight.Visibility = Visibility.Visible;
        rtbUnified.Visibility = Visibility.Collapsed;
    }

    private void ShowUnified()
    {
        rtbLeft.Visibility = Visibility.Collapsed;
        rtbRight.Visibility = Visibility.Collapsed;
        rtbUnified.Visibility = Visibility.Visible;
    }

    private void btnLowercase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (rtbUnified.Visibility == Visibility.Visible)
            {
                var text = new TextRange(rtbUnified.Document.ContentStart, rtbUnified.Document.ContentEnd).Text;
                rtbUnified.Document.Blocks.Clear();
                var para = new Paragraph(new Run(text.ToLower()));
                rtbUnified.Document.Blocks.Add(para);
            }
            else
            {
                var leftText = new TextRange(rtbLeft.Document.ContentStart, rtbLeft.Document.ContentEnd).Text;
                var rightText = new TextRange(rtbRight.Document.ContentStart, rtbRight.Document.ContentEnd).Text;
                
                rtbLeft.Document.Blocks.Clear();
                rtbLeft.Document.Blocks.Add(new Paragraph(new Run(leftText.ToLower())));
                
                rtbRight.Document.Blocks.Clear();
                rtbRight.Document.Blocks.Add(new Paragraph(new Run(rightText.ToLower())));
            }
            statusText.Text = "Converted to lowercase.";
        }
        catch (Exception ex)
        {
            statusText.Text = $"Error: {ex.Message}";
        }
    }

    private void btnSortLines_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (rtbUnified.Visibility == Visibility.Visible)
            {
                var text = new TextRange(rtbUnified.Document.ContentStart, rtbUnified.Document.ContentEnd).Text;
                var sorted = string.Join("\n", text.Split('\n').OrderBy(l => l));
                rtbUnified.Document.Blocks.Clear();
                rtbUnified.Document.Blocks.Add(new Paragraph(new Run(sorted)));
            }
            else
            {
                var leftText = new TextRange(rtbLeft.Document.ContentStart, rtbLeft.Document.ContentEnd).Text;
                var rightText = new TextRange(rtbRight.Document.ContentStart, rtbRight.Document.ContentEnd).Text;
                
                var leftSorted = string.Join("\n", leftText.Split('\n').OrderBy(l => l));
                var rightSorted = string.Join("\n", rightText.Split('\n').OrderBy(l => l));
                
                rtbLeft.Document.Blocks.Clear();
                rtbLeft.Document.Blocks.Add(new Paragraph(new Run(leftSorted)));
                
                rtbRight.Document.Blocks.Clear();
                rtbRight.Document.Blocks.Add(new Paragraph(new Run(rightSorted)));
            }
            statusText.Text = "Lines sorted A-Z.";
        }
        catch (Exception ex)
        {
            statusText.Text = $"Error: {ex.Message}";
        }
    }

    private void btnTrimWhitespace_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (rtbUnified.Visibility == Visibility.Visible)
            {
                var text = new TextRange(rtbUnified.Document.ContentStart, rtbUnified.Document.ContentEnd).Text;
                var trimmed = string.Join("\n", text.Split('\n').Select(l => l.Trim()));
                rtbUnified.Document.Blocks.Clear();
                rtbUnified.Document.Blocks.Add(new Paragraph(new Run(trimmed)));
            }
            else
            {
                var leftText = new TextRange(rtbLeft.Document.ContentStart, rtbLeft.Document.ContentEnd).Text;
                var rightText = new TextRange(rtbRight.Document.ContentStart, rtbRight.Document.ContentEnd).Text;
                
                var leftTrimmed = string.Join("\n", leftText.Split('\n').Select(l => l.Trim()));
                var rightTrimmed = string.Join("\n", rightText.Split('\n').Select(l => l.Trim()));
                
                rtbLeft.Document.Blocks.Clear();
                rtbLeft.Document.Blocks.Add(new Paragraph(new Run(leftTrimmed)));
                
                rtbRight.Document.Blocks.Clear();
                rtbRight.Document.Blocks.Add(new Paragraph(new Run(rightTrimmed)));
            }
            statusText.Text = "Whitespace trimmed.";
        }
        catch (Exception ex)
        {
            statusText.Text = $"Error: {ex.Message}";
        }
    }

    private void btnExportPDF_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.MessageBox.Show("PDF export feature will be available in a future update.\n\nUse 'Save as file' to export as text.", "PDF Export");
            statusText.Text = "PDF export coming soon.";
        }
        catch (Exception ex)
        {
            statusText.Text = $"Error: {ex.Message}";
        }
    }
}