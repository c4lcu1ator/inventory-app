using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace InventoryApp;

/// <summary>
/// Диалог добавления и редактирования выдачи.
/// </summary>
public class IssueForm : Form
{
    private readonly List<Issue> _all;
    private readonly TextBox _number = new();
    private readonly TextBox _item = new();
    private readonly TextBox _recipient = new();
    private readonly DateTimePicker _date = new();
    private readonly Label _due = new();
    private readonly Label _errorNumber = new();
    private readonly Label _errorItem = new();
    private readonly Label _errorRecipient = new();
    private readonly Label _errorDate = new();

    /// <summary>
    /// Результат: новая запись либо та же запись, что редактировали.
    /// </summary>
    public Issue Result { get; }

    public IssueForm(Issue? editing, List<Issue> all)
    {
        _all = all;
        Result = editing ?? new Issue();

        Text = editing == null ? "Новая выдача" : "Редактирование выдачи";
        Width = 660;
        Height = 380;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 6; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        int row = 0;
        AddRow(table, row++, "Инв. номер:", _number, _errorNumber);
        AddRow(table, row++, "Предмет:", _item, _errorItem);
        AddRow(table, row++, "Получатель:", _recipient, _errorRecipient);

        _date.Format = DateTimePickerFormat.Custom;
        _date.CustomFormat = "dd.MM.yyyy";
        _date.ValueChanged += (s, e) => UpdateDueLabel();
        AddRow(table, row++, "Дата выдачи:", _date, _errorDate);

        table.Controls.Add(new Label
        {
            Text = "Срок возврата:", AutoSize = true, Margin = new Padding(3, 8, 3, 3)
        }, 0, row);
        _due.AutoSize = true;
        _due.Margin = new Padding(3, 8, 3, 3);
        table.Controls.Add(_due, 1, row);
        row++;

        var save = new Button { Text = "Сохранить", Width = 100, Height = 30 };
        save.Click += SaveClick;
        var cancel = new Button
        {
            Text = "Отмена", Width = 100, Height = 30, DialogResult = DialogResult.Cancel
        };
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0)
        };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        table.Controls.Add(buttons, 0, row);
        table.SetColumnSpan(buttons, 3);

        Controls.Add(table);
        AcceptButton = save;
        CancelButton = cancel; // Esc / «Отмена» = DialogResult.Cancel, данные не применяются

        if (editing != null)
        {
            _number.Text = editing.InventoryNumber;
            _item.Text = editing.ItemName;
            _recipient.Text = editing.Recipient;
            _date.Value = editing.IssueDate.Date;
        }
        else
        {
            _number.Text = Variant.Prefix + "-";
            _date.Value = DateTime.Today;
        }

        UpdateDueLabel();
    }

    private static void AddRow(TableLayoutPanel table, int row, string caption,
        Control input, Label error)
    {
        table.Controls.Add(new Label
        {
            Text = caption, AutoSize = true, Margin = new Padding(3, 8, 3, 3)
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 4, 3, 4);
        table.Controls.Add(input, 1, row);

        error.AutoSize = true;
        error.ForeColor = Color.Firebrick;
        error.Margin = new Padding(3, 8, 3, 3);
        table.Controls.Add(error, 2, row);
    }

    /// <summary>
    /// Пересчитывает и показывает срок возврата при смене даты выдачи.
    /// </summary>
    private void UpdateDueLabel()
    {
        _due.Text = Variant.CalcDueDate(_date.Value).ToString("dd.MM.yyyy");
    }

    private void ClearErrors()
    {
        _errorNumber.Text = "";
        _errorItem.Text = "";
        _errorRecipient.Text = "";
        _errorDate.Text = "";
    }

    /// <summary>
    /// Проверка данных. При ошибке форма остаётся открытой.
    /// </summary>
    private void SaveClick(object? sender, EventArgs e)
    {
        ClearErrors();
        bool ok = true;

        string number = _number.Text.Trim();

        if (!Variant.IsValidNumber(number))
        {
            _errorNumber.Text = $"Формат: {Variant.Example}";
            ok = false;
        }
        else if (_all.Any(x => x != Result
                               && x.InventoryNumber == number
                               && x.ReturnedDate == null))
        {
            _errorNumber.Text = "По этому номеру уже есть открытая выдача";
            ok = false;
        }

        if (_item.Text.Trim().Length == 0)
        {
            _errorItem.Text = "Введите название";
            ok = false;
        }

        if (_recipient.Text.Trim().Length == 0)
        {
            _errorRecipient.Text = "Введите получателя";
            ok = false;
        }

        if (_date.Value.Date > DateTime.Today)
        {
            _errorDate.Text = "Дата не может быть в будущем";
            ok = false;
        }

        if (!ok) return;

        Result.InventoryNumber = number;
        Result.ItemName = _item.Text.Trim();
        Result.Recipient = _recipient.Text.Trim();
        Result.IssueDate = _date.Value.Date;
        Result.DueDate = Variant.CalcDueDate(_date.Value);

        DialogResult = DialogResult.OK;
    }
}
