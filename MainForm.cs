using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace InventoryApp;

/// <summary>
/// Главное окно: таблица записей, поиск, фильтр по статусу, кнопки действий.
/// </summary>
public class MainForm : Form
{
    private readonly List<Issue> _all;                 // все записи
    private readonly BindingList<Issue> _view = new(); // то, что видно в таблице
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _filter = new();

    public MainForm()
    {
        _all = IssueStorage.Load();

        Text = "Журнал выдачи оборудования";
        Width = 1100;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        RefreshGrid();
    }

    private void BuildUi()
    {
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(8, 6, 8, 6)
        };

        top.Controls.Add(new Label { Text = "Поиск:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) });

        _search.Width = 200;
        _search.Margin = new Padding(0, 7, 0, 0);
        _search.TextChanged += (s, e) => RefreshGrid();
        top.Controls.Add(_search);

        top.Controls.Add(new Label { Text = "Статус:", AutoSize = true, Margin = new Padding(16, 10, 4, 0) });

        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.Width = 140;
        _filter.Margin = new Padding(0, 7, 0, 0);
        _filter.Items.AddRange(new object[] { "Все", "Выдано", "Просрочено", "Возвращено" });
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (s, e) => RefreshGrid();
        top.Controls.Add(_filter);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 44,
            Padding = new Padding(8, 4, 8, 4)
        };
        bottom.Controls.Add(MakeButton("Добавить", AddClick));
        bottom.Controls.Add(MakeButton("Изменить", EditClick));
        bottom.Controls.Add(MakeButton("Вернуть", ReturnClick));

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditClick(s, e); };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(bottom, 0, 2);

        Controls.Add(root);
    }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, Width = 110, Height = 32, Margin = new Padding(4) };
        button.Click += onClick;
        return button;
    }

    /// <summary>
    /// Поиск + фильтр по статусу + подсветка просроченных строк.
    /// </summary>
    private void RefreshGrid()
    {
        string text = _search.Text.Trim();
        string status = _filter.SelectedItem as string ?? "Все";

        var rows = _all
            .Where(issue =>
                (text.Length == 0
                 || issue.Recipient.Contains(text, StringComparison.OrdinalIgnoreCase)
                 || issue.InventoryNumber.Contains(text, StringComparison.OrdinalIgnoreCase))
                && (status == "Все" || issue.Status == status))
            .OrderBy(issue => issue.DueDate)
            .ToList();

        _view.Clear();
        foreach (var issue in rows) _view.Add(issue);

        _grid.DataSource = _view;

        if (_grid.Columns["Id"] != null) _grid.Columns["Id"].Visible = false;
        if (_grid.Columns["ReturnedDate"] != null) _grid.Columns["ReturnedDate"].Visible = false;

        SetHeader("InventoryNumber", "Инв. номер");
        SetHeader("ItemName", "Предмет");
        SetHeader("Recipient", "Получатель");
        SetHeader("IssueDate", "Дата выдачи");
        SetHeader("DueDate", "Срок возврата");
        SetHeader("Status", "Статус");

        foreach (var name in new[] { "IssueDate", "DueDate" })
            if (_grid.Columns[name] != null)
                _grid.Columns[name].DefaultCellStyle.Format = "dd.MM.yyyy";

        foreach (DataGridViewRow row in _grid.Rows)
            if (row.DataBoundItem is Issue { Status: "Просрочено" })
                row.DefaultCellStyle.BackColor = Color.MistyRose;
    }

    private void SetHeader(string column, string title)
    {
        if (_grid.Columns[column] != null) _grid.Columns[column].HeaderText = title;
    }

    private void AddClick(object? sender, EventArgs e)
    {
        using var form = new IssueForm(null, _all);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        form.Result.Id = _all.Count == 0 ? 1 : _all.Max(x => x.Id) + 1;
        _all.Add(form.Result);
        IssueStorage.Save(_all);
        RefreshGrid();
    }

    private void EditClick(object? sender, EventArgs e)
    {
        var issue = GetSelectedIssue();
        if (issue == null)
        {
            MessageBox.Show("Сначала выберите запись в таблице.");
            return;
        }

        using var form = new IssueForm(issue, _all);
        if (form.ShowDialog(this) != DialogResult.OK) return; // отмена — запись не менялась

        IssueStorage.Save(_all);
        RefreshGrid();
    }

    private void ReturnClick(object? sender, EventArgs e)
    {
        var issue = GetSelectedIssue();
        if (issue == null)
        {
            MessageBox.Show("Сначала выберите запись в таблице.");
            return;
        }
        if (issue.ReturnedDate != null)
        {
            MessageBox.Show("Этот предмет уже возвращён.");
            return;
        }

        var answer = MessageBox.Show(
            $"Вернуть «{issue.ItemName}» от {issue.Recipient}?",
            "Возврат", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        issue.ReturnedDate = DateTime.Today;
        IssueStorage.Save(_all);
        RefreshGrid();
    }

    private Issue? GetSelectedIssue()
    {
        return _grid.CurrentRow?.DataBoundItem as Issue;
    }
}
