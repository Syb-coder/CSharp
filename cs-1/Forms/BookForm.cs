using LibraryManagement.BLL;
using LibraryManagement.Models;

namespace LibraryManagement.Forms;

/// <summary>
/// 图书管理窗体：图书的查询、新增、修改、删除
/// 普通用户仅可查询；管理员可增删改查
/// </summary>
public class BookForm : Form
{
    private readonly BookService _bookService = new();
    private readonly BookCategoryService _categoryService = new();
    private readonly User _currentUser;

    // 查询区控件
    private readonly TextBox _txtSearchBookName;
    private readonly TextBox _txtSearchAuthor;
    private readonly ComboBox _cboSearchCategory;
    private readonly TextBox _txtSearchISBN;
    private readonly Button _btnSearch;

    // 列表控件
    private readonly DataGridView _dgvBooks;

    // 输入区控件
    private readonly GroupBox _grpInput;
    private readonly TextBox _txtBookID;
    private readonly TextBox _txtBookName;
    private readonly TextBox _txtAuthor;
    private readonly TextBox _txtPublisher;
    private readonly DateTimePicker _dtpPublishDate;
    private readonly TextBox _txtISBN;
    private readonly TextBox _txtPrice;
    private readonly ComboBox _cboCategory;
    private readonly TextBox _txtTotalCount;

    // 操作按钮
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private readonly Button _btnClear;
    private readonly Button _btnBack;

    /// <summary>
    /// 构造图书管理窗体
    /// </summary>
    /// <param name="currentUser">当前登录用户，用于权限控制</param>
    public BookForm(User currentUser)
    {
        _currentUser = currentUser;

        // 窗体基本属性
        Text = "图书管理";
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // ===== 查询区 =====
        Label lblSearchBookName = new()
        {
            Text = "书名：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 18),
            AutoSize = true
        };
        _txtSearchBookName = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(60, 15),
            Size = new Size(130, 25)
        };

        Label lblSearchAuthor = new()
        {
            Text = "作者：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(205, 18),
            AutoSize = true
        };
        _txtSearchAuthor = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(250, 15),
            Size = new Size(130, 25)
        };

        Label lblSearchCategory = new()
        {
            Text = "类别：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(395, 18),
            AutoSize = true
        };
        _cboSearchCategory = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(440, 15),
            Size = new Size(140, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblSearchISBN = new()
        {
            Text = "ISBN：",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(595, 18),
            AutoSize = true
        };
        _txtSearchISBN = new TextBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(640, 15),
            Size = new Size(130, 25)
        };

        _btnSearch = CreateButton("查询", 780, 13);
        _btnSearch.Click += (s, e) => LoadData();

        // ===== 列表区 =====
        _dgvBooks = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(860, 280),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _dgvBooks.SelectionChanged += DgvBooks_SelectionChanged;

        // ===== 输入区 =====
        _grpInput = new GroupBox
        {
            Text = "图书信息",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(15, 335),
            Size = new Size(860, 170)
        };

        // 第一行：图书编号、书名、作者、出版社
        Label lblBookID = new() { Text = "图书编号：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(20, 33), AutoSize = true };
        _txtBookID = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(85, 30), Size = new Size(140, 25) };

        Label lblBookName = new() { Text = "书名：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(240, 33), AutoSize = true };
        _txtBookName = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(285, 30), Size = new Size(180, 25) };

        Label lblAuthor = new() { Text = "作者：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(480, 33), AutoSize = true };
        _txtAuthor = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(525, 30), Size = new Size(140, 25) };

        Label lblPublisher = new() { Text = "出版社：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(680, 33), AutoSize = true };
        _txtPublisher = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(740, 30), Size = new Size(110, 25) };

        // 第二行：出版日期、ISBN、价格、类别
        Label lblPublishDate = new() { Text = "出版日期：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(20, 71), AutoSize = true };
        _dtpPublishDate = new DateTimePicker
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(90, 68),
            Size = new Size(130, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            ShowCheckBox = true,
            Checked = false
        };

        Label lblISBN = new() { Text = "ISBN：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(235, 71), AutoSize = true };
        _txtISBN = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(280, 68), Size = new Size(140, 25) };

        Label lblPrice = new() { Text = "价格：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(435, 71), AutoSize = true };
        _txtPrice = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(480, 68), Size = new Size(80, 25) };

        Label lblCategory = new() { Text = "类别：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(575, 71), AutoSize = true };
        _cboCategory = new ComboBox
        {
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(620, 68),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 第三行：馆藏数量 + 操作按钮
        Label lblTotalCount = new() { Text = "馆藏数量：", Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(20, 111), AutoSize = true };
        _txtTotalCount = new TextBox { Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(90, 108), Size = new Size(80, 25) };

        _btnAdd = CreateButton("添加", 300, 108);
        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate = CreateButton("修改", 390, 108);
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete = CreateButton("删除", 480, 108);
        _btnDelete.Click += BtnDelete_Click;
        _btnClear = CreateButton("清空", 570, 108);
        _btnClear.Click += (s, e) => ClearInput();

        _grpInput.Controls.AddRange(new Control[]
        {
            lblBookID, _txtBookID,
            lblBookName, _txtBookName,
            lblAuthor, _txtAuthor,
            lblPublisher, _txtPublisher,
            lblPublishDate, _dtpPublishDate,
            lblISBN, _txtISBN,
            lblPrice, _txtPrice,
            lblCategory, _cboCategory,
            lblTotalCount, _txtTotalCount,
            _btnAdd, _btnUpdate, _btnDelete, _btnClear
        });

        // 返回按钮
        _btnBack = CreateButton("返回", 780, 515);
        _btnBack.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearchBookName, _txtSearchBookName,
            lblSearchAuthor, _txtSearchAuthor,
            lblSearchCategory, _cboSearchCategory,
            lblSearchISBN, _txtSearchISBN,
            _btnSearch,
            _dgvBooks,
            _grpInput,
            _btnBack
        });

        // 窗体加载时先加载类别下拉框，再加载图书列表
        Load += (s, e) =>
        {
            LoadCategories();
            LoadData();
        };

        // 根据权限启用/禁用操作按钮
        ApplyPermission();
    }

    /// <summary>
    /// 加载图书类别到查询与输入下拉框
    /// </summary>
    private void LoadCategories()
    {
        try
        {
            List<BookCategory> categories = _categoryService.GetAllCategories();

            // 查询下拉框：首项"全部"，空编号表示不按类别过滤
            List<BookCategory> searchSource = new()
            {
                new BookCategory { CategoryID = "", CategoryName = "全部" }
            };
            searchSource.AddRange(categories);
            _cboSearchCategory.DisplayMember = nameof(BookCategory.CategoryName);
            _cboSearchCategory.ValueMember = nameof(BookCategory.CategoryID);
            _cboSearchCategory.DataSource = searchSource;

            // 输入下拉框：首项"请选择"，空编号触发业务校验
            // 使用独立列表实例，避免与查询下拉框共享 BindingContext 导致选中联动
            List<BookCategory> inputSource = new()
            {
                new BookCategory { CategoryID = "", CategoryName = "请选择" }
            };
            inputSource.AddRange(categories);
            _cboCategory.DisplayMember = nameof(BookCategory.CategoryName);
            _cboCategory.ValueMember = nameof(BookCategory.CategoryID);
            _cboCategory.DataSource = inputSource;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载类别失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 按查询条件加载图书列表；四个条件均为空时加载全部
    /// </summary>
    private void LoadData()
    {
        try
        {
            string bookName = _txtSearchBookName.Text.Trim();
            string author = _txtSearchAuthor.Text.Trim();
            string categoryID = _cboSearchCategory.SelectedValue?.ToString() ?? "";
            string isbn = _txtSearchISBN.Text.Trim();

            List<Book> books;
            // 无任何条件时直接加载全部，避免无意义的条件查询
            if (string.IsNullOrEmpty(bookName) && string.IsNullOrEmpty(author)
                && string.IsNullOrEmpty(categoryID) && string.IsNullOrEmpty(isbn))
            {
                books = _bookService.GetAllBooks();
            }
            else
            {
                books = _bookService.SearchBooks(bookName, author, categoryID, isbn);
            }

            _dgvBooks.DataSource = books;
            SetColumnHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 设置 DataGridView 列标题、格式与显示顺序
    /// </summary>
    private void SetColumnHeaders()
    {
        // 隐藏外键列，仅展示类别名称
        SetColumn(nameof(Book.CategoryID), null);
        SetColumn(nameof(Book.BookID), "图书编号");
        SetColumn(nameof(Book.BookName), "书名");
        SetColumn(nameof(Book.Author), "作者");
        SetColumn(nameof(Book.Publisher), "出版社");
        SetColumn(nameof(Book.PublishDate), "出版日期");
        SetColumn(nameof(Book.ISBN), "ISBN");
        SetColumn(nameof(Book.Price), "价格");
        SetColumn(nameof(Book.CategoryName), "类别");
        SetColumn(nameof(Book.TotalCount), "馆藏数量");
        SetColumn(nameof(Book.AvailableCount), "可借数量");

        // 日期与价格格式化，避免显示时间部分与多余小数位
        if (_dgvBooks.Columns.Contains(nameof(Book.PublishDate)))
        {
            _dgvBooks.Columns[nameof(Book.PublishDate)].DefaultCellStyle.Format = "yyyy-MM-dd";
        }
        if (_dgvBooks.Columns.Contains(nameof(Book.Price)))
        {
            _dgvBooks.Columns[nameof(Book.Price)].DefaultCellStyle.Format = "0.00";
        }

        // 自动生成顺序中 CategoryName 在最后，将其调整到"价格"之后，符合需求列顺序
        if (_dgvBooks.Columns.Contains(nameof(Book.CategoryName)))
        {
            _dgvBooks.Columns[nameof(Book.CategoryName)].DisplayIndex = 7;
        }
    }

    /// <summary>
    /// 设置单列标题或隐藏列
    /// </summary>
    /// <param name="propName">属性名</param>
    /// <param name="headerText">列标题；为 null 表示隐藏该列</param>
    private void SetColumn(string propName, string headerText)
    {
        if (!_dgvBooks.Columns.Contains(propName))
        {
            return;
        }
        if (headerText == null)
        {
            _dgvBooks.Columns[propName].Visible = false;
        }
        else
        {
            _dgvBooks.Columns[propName].HeaderText = headerText;
        }
    }

    /// <summary>
    /// 列表选中行变化时回填输入框，并将图书编号置为只读
    /// </summary>
    private void DgvBooks_SelectionChanged(object sender, EventArgs e)
    {
        // 绑定切换瞬间 DataBoundItem 可能为 null，需做类型守卫避免空引用
        if (_dgvBooks.CurrentRow?.DataBoundItem is not Book book)
        {
            return;
        }

        _txtBookID.Text = book.BookID;
        _txtBookName.Text = book.BookName;
        _txtAuthor.Text = book.Author;
        _txtPublisher.Text = book.Publisher;
        _txtISBN.Text = book.ISBN;
        _txtPrice.Text = book.Price.HasValue ? book.Price.Value.ToString() : "";
        _txtTotalCount.Text = book.TotalCount.ToString();

        // 出版日期可能为空或超出 DateTimePicker 范围，需做边界校验避免越界异常
        if (book.PublishDate.HasValue
            && book.PublishDate.Value >= _dtpPublishDate.MinDate
            && book.PublishDate.Value <= _dtpPublishDate.MaxDate)
        {
            _dtpPublishDate.Checked = true;
            _dtpPublishDate.Value = book.PublishDate.Value;
        }
        else
        {
            _dtpPublishDate.Checked = false;
        }

        // 回填类别下拉框；未匹配时保持原选择，不抛异常
        _cboCategory.SelectedValue = book.CategoryID;

        // 编号为主键，选中后不可编辑
        _txtBookID.ReadOnly = true;
        _txtBookID.BackColor = Color.FromArgb(240, 240, 240);
    }

    /// <summary>
    /// 从输入框构造图书实体，并完成数值字段解析
    /// </summary>
    /// <returns>填充好的图书实体</returns>
    /// <exception cref="BusinessException">价格或数量格式不合法</exception>
    private Book BuildBookFromInput()
    {
        // 解析价格（允许为空）
        decimal? price = null;
        string priceText = _txtPrice.Text.Trim();
        if (!string.IsNullOrEmpty(priceText))
        {
            if (!decimal.TryParse(priceText, out decimal priceValue))
            {
                throw new BusinessException("价格格式不正确");
            }
            price = priceValue;
        }

        // 解析馆藏数量
        if (!int.TryParse(_txtTotalCount.Text.Trim(), out int totalCount))
        {
            throw new BusinessException("馆藏数量必须为整数");
        }

        return new Book
        {
            BookID = _txtBookID.Text.Trim(),
            BookName = _txtBookName.Text.Trim(),
            Author = _txtAuthor.Text.Trim(),
            Publisher = _txtPublisher.Text.Trim(),
            PublishDate = _dtpPublishDate.Checked ? _dtpPublishDate.Value : null,
            ISBN = _txtISBN.Text.Trim(),
            Price = price,
            CategoryID = _cboCategory.SelectedValue?.ToString() ?? "",
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void BtnAdd_Click(object sender, EventArgs e)
    {
        try
        {
            Book book = BuildBookFromInput();
            _bookService.AddBook(book);
            MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 修改按钮点击事件
    /// </summary>
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        if (_dgvBooks.CurrentRow?.DataBoundItem is not Book selected)
        {
            MessageBox.Show("请先选择要修改的图书", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Book book = BuildBookFromInput();
            // 编号以选中行为准，防止输入框被篡改导致主键漂移
            book.BookID = selected.BookID;
            _bookService.UpdateBook(book);
            MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvBooks.CurrentRow?.DataBoundItem is not Book selected)
        {
            MessageBox.Show("请先选择要删除的图书", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show($"确定要删除图书《{selected.BookName}》吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _bookService.DeleteBook(selected.BookID);
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
            LoadData();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 清空输入框并重置编号可编辑状态
    /// </summary>
    private void ClearInput()
    {
        _txtBookID.Clear();
        _txtBookName.Clear();
        _txtAuthor.Clear();
        _txtPublisher.Clear();
        _txtISBN.Clear();
        _txtPrice.Clear();
        _txtTotalCount.Clear();
        _dtpPublishDate.Checked = false;
        // 输入类别下拉框回到"请选择"占位项
        if (_cboCategory.Items.Count > 0)
        {
            _cboCategory.SelectedIndex = 0;
        }
        _txtBookID.ReadOnly = false;
        _txtBookID.BackColor = Color.White;
        if (_dgvBooks.CurrentRow != null)
        {
            _dgvBooks.ClearSelection();
        }
    }

    /// <summary>
    /// 根据登录用户权限启用/禁用操作按钮
    /// 普通用户禁用添加/修改/删除并变灰；查询、清空、返回不受影响
    /// </summary>
    private void ApplyPermission()
    {
        bool isAdmin = _currentUser != null && _currentUser.UserPurview == BusinessConstants.ROLE_ADMIN;
        if (isAdmin)
        {
            return;
        }

        _btnAdd.Enabled = false;
        _btnUpdate.Enabled = false;
        _btnDelete.Enabled = false;
        _btnAdd.BackColor = Color.FromArgb(200, 200, 200);
        _btnUpdate.BackColor = Color.FromArgb(200, 200, 200);
        _btnDelete.BackColor = Color.FromArgb(200, 200, 200);
    }

    /// <summary>
    /// 创建统一风格的按钮：蓝色背景、白色文字、扁平样式
    /// </summary>
    /// <param name="text">按钮文本</param>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <returns>按钮实例</returns>
    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 9F),
            Size = new Size(80, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
