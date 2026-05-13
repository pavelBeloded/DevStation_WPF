# DevStation — CLAUDE.md

Это файл с инструкциями для Claude Code. Читай его ПОЛНОСТЬЮ перед любыми изменениями UI.

---

## 📁 Структура проекта

```
DevStation/
├── Data/           → Entity Framework (Models, DbContext, Migrations)
├── Services/       → Бизнес-логика (Interfaces + Implementations)
├── ViewModels/     → MVVM ViewModels (Base/, LoginViewModel, etc.)
├── Views/          → WPF Windows и Pages
│   ├── Windows/    → LoginWindow, MainWindow
│   └── Pages/      → SvgConverterPage, ImageOptimizerPage, etc.
├── Resources/
│   └── Styles/
│       └── DevStationTheme.xaml   ← ВСЕ стили и цвета здесь
├── Utils/          → ClipboardHelper, ValidationHelper
├── App.xaml        → Merge DevStationTheme.xaml здесь
└── appsettings.json
```

---

## 🎨 Design System — ОБЯЗАТЕЛЬНЫЕ ПРАВИЛА

### ПРАВИЛО №1: ТОЛЬКО через StaticResource
```xml
<!-- ✅ ПРАВИЛЬНО -->
<Border Background="{StaticResource CardBackground}">
<TextBlock Foreground="{StaticResource TextPrimary}">
<Button Background="{StaticResource AccentBrush}">

<!-- ❌ ЗАПРЕЩЕНО — никаких хардкоженых значений -->
<Border Background="#1A1A1A">
<TextBlock Foreground="White">
<Button Background="#F5A623">
```

### ПРАВИЛО №2: Все стили в DevStationTheme.xaml
Ни один компонент не должен иметь инлайн-стили кроме позиционирования (Margin, Width, Height).

### ПРАВИЛО №3: Архитектура ResourceDictionary
```xml
<!-- DevStationTheme.xaml структура -->
<!-- 1. Colors -->
<!-- 2. Brushes -->
<!-- 3. Base Styles (Window, Page) -->
<!-- 4. Typography Styles -->
<!-- 5. Button Styles -->
<!-- 6. Input Styles -->
<!-- 7. Card Styles -->
<!-- 8. Sidebar Styles -->
<!-- 9. Table Styles -->
<!-- 10. Code Editor Styles -->
```

---

## 🎨 Цвета (копировать точно)

### Фоны
```
AppBackground:      #121212   — основной фон окна
SidebarBackground:  #181818   — боковая панель
CardBackground:     #181818   — все карточки
HeaderBackground:   #121212   — топ-бар
InputBackground:    #181818   — поля ввода
DropZoneBackground: #181818   — drag & drop зоны
CodeBackground:     #0C0C0C   — редактор кода
```

### Акцент
```
AccentPrimary:      #FFB86C   — основной оранжевый (кнопки, иконки, активное меню)
AccentHover:        #E8961A   — hover кнопок
AccentMuted:        #FFB86C1A — прозрачный акцент (фон иконок Quick Access)
```

### Текст
```
TextPrimary:        #FFFFFF   — заголовки
TextSecondary:      #6B6B6B   — подписи, мета
TextMuted:          #4A4A4A   — placeholder
TextAccent:         #F5A623   — активный пункт меню
TextLabel:          #9A9A9A   — лейблы полей
TextCode:           #E8E8E8   — код
TextCodeKeyword:    #FFB86C   — ключевые слова кода (import, export, const)
TextCodeString:     #CE9178   — строки в коде
TextCodeComment:    #6A9955   — комментарии в коде
```

### Границы
```
BorderDefault:      #2A2A2A   — стандартные карточки
BorderSubtle:       #222222   — разделители
BorderAccent:       #FFB86C   — активный пункт меню (3px слева)
BorderDashed:       #3A3A3A   — drop zone пунктирная граница
BorderInput:        #2E2E2E   — поля ввода
```

### Статусы
```
StatusActive:       #FFB86C   — COMPLETED, OPTIMIZING прогресс
StatusQueued:       #4A4A4A   — QUEUED
StatusReady:        #22C55E   — Ready to Export
```

---

## 📐 Размеры (не меняй без причины)

### Layout
```
SidebarWidth:       300px
TopBarHeight:       64x
ContentPaddingH:    40px
ContentPaddingV:    32px
CardBorderRadius:   8px
CardGap:            16px
```

### Кнопки
```
ButtonHeightPrimary: 40px
ButtonHeightSmall:   32px
ButtonBorderRadius:  6px
ButtonFontSize:      13px
ButtonFontWeight:    600
```

### Поля ввода
```
InputHeight:        40px
InputBorderRadius:  6px
InputFontSize:      14px
```

### Таблица
```
TableRowHeight:     56px
ProgressBarHeight:  3px
ThumbnailSize:      40px
TableHeaderCase:    UPPERCASE
TableHeaderSize:    11px
TableLabelSpacing:  1px (letter-spacing)
```

---

## 🧩 Шаблоны компонентов

### Структура страницы
```xml
<Grid Background="{StaticResource AppBackground}">
    <StackPanel Margin="40,32">
        <!-- Заголовок страницы -->
        <TextBlock Text="Page Title" Style="{StaticResource PageTitle}"/>
        <TextBlock Text="Subtitle text" Style="{StaticResource PageSubtitle}"/>

        <!-- Контент -->
        <Border Style="{StaticResource Card}" Margin="0,24,0,0">
            ...
        </Border>
    </StackPanel>
</Grid>
```

### Карточка (Card)
```xml
<Border Style="{StaticResource Card}">
    <StackPanel>
        <TextBlock Text="Card Title" Style="{StaticResource CardTitle}"/>
        <TextBlock Text="Description" Style="{StaticResource BodyText}"/>
    </StackPanel>
</Border>
```

### Основная кнопка (оранжевая)
```xml
<Button Content="CHECK" Style="{StaticResource PrimaryButton}"/>
```

### Outlined кнопка
```xml
<Button Content="Filter" Style="{StaticResource OutlinedButton}"/>
```

### Поле ввода
```xml
<TextBox Style="{StaticResource DefaultInput}" 
         Text="{Binding Property, UpdateSourceTrigger=PropertyChanged}"/>
```

### Пункт меню (Sidebar)
```xml
<!-- Активный -->
<Border Style="{StaticResource SidebarItemActive}">
    <StackPanel Orientation="Horizontal">
        <Image Source="..." Style="{StaticResource SidebarIcon}"/>
        <TextBlock Text="Home" Style="{StaticResource SidebarItemTextActive}"/>
    </StackPanel>
</Border>

<!-- Неактивный -->
<Border Style="{StaticResource SidebarItem}">
    ...
</Border>
```

### Drop Zone
```xml
<Border Style="{StaticResource DropZone}">
    <!-- AllowDrop="True" обязательно -->
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
        <!-- иконка -->
        <TextBlock Text="Drop SVG here" Style="{StaticResource DropZoneTitle}"/>
        <TextBlock Text="or Select File from directory" Style="{StaticResource DropZoneSubtitle}"/>
    </StackPanel>
</Border>
```

---

## 🏗️ MVVM Правила

### ViewModel
- Наследовать от `ViewModelBase`
- Все свойства через `SetProperty(ref _field, value)`
- Команды через `RelayCommand` или `RelayCommand<T>`
- Асинхронные операции: `async Task`, не `async void` (кроме событий)

### View (XAML)
- Binding через `{Binding PropertyName}`
- Команды: `Command="{Binding CommandName}"`
- Никакой бизнес-логики в code-behind
- Исключение: drag & drop события (допустимо в code-behind)

### Services
- Всегда использовать интерфейс: `IXxxService`
- Получать через конструктор (DI)
- Методы работы с БД: `async Task<T>`

---

## 📋 Архитектура страниц

### Страница SVG Converter (Converter)
- Layout: два блока рядом (50/50) на всю высоту
- Левый: Source SVG + drag&drop + тип файла badges
- Правый: редактор результата + syntax highlighting + Copy/Download

### Страница MDN Search
- Layout: два блока рядом (40/60)
- Левый: поле поиска + список результатов (ScrollViewer с карточками)
- Правый: детальный просмотр выбранного результата + кнопка Copy Summary
- Стратегия данных: cache-first
  1. Запрос → ищем в MdnCache таблице (MSSQL)
  2. Найдено в кэше → показываем мгновенно
  3. Не найдено → GET https://developer.mozilla.org/api/v1/search?q={query}&locale=en-US
  4. Получили результат → сохраняем в MdnCache → показываем
- Debounce поиска: 400ms (не делать запрос на каждый символ)
- Карточка результата: Title (CardTitleSmall) + Category badge + Summary (CardBody, 2 строки)
- При выборе карточки → правая панель показывает полный Summary + ссылку на MDN

### Страница Image Optimizer
- Drag zone сверху (большая, пунктирная оранжевая граница)
- Таблица снизу: OPTIMIZATION QUEUE с прогрессом

### Страница Mock Data Generator
- Два блока рядом (40/60)
- Левый: Configuration form
- Правый: Output.json с кодом

### Страница Cloud Snippets
- Заголовок + Filter/New Snippet кнопки
- Сетка сниппетов (2 колонки) + Admin Panel виджет справа (если admin)

---

## ⚠️ Частые ошибки — НЕ ДЕЛАЙ

1. ❌ `Background="Black"` — только через `{StaticResource AppBackground}`
2. ❌ Стили инлайн в XAML файлах страниц — только в DevStationTheme.xaml
3. ❌ `FontFamily="Arial"` — используй `FontFamily="Segoe UI"` (системный)
4. ❌ Жёсткие размеры без причины — используй `HorizontalAlignment="Stretch"`
5. ❌ `MessageBox.Show()` для ошибок — используй binding на ErrorMessage в ViewModel
6. ❌ Бизнес-логика в code-behind — только в ViewModel и Services
7. ❌ `new MyService()` в ViewModel — только через конструктор (DI)

---

## 🔧 Технический стек

```
Framework:      .NET 8 WPF
ORM:            Entity Framework Core 8.0
DB:             MSSQL Server (LocalDB для разработки)
DI:             Microsoft.Extensions.DependencyInjection
SVG:            Svg NuGet (парсинг SVG)
Image:          ImageMagick.NET (конвертация в WebP)
Mock Data:      Bogus (генерация данных)
MDN:            cache-first: MdnCache (MSSQL) → MDN API (developer.mozilla.org/api/v1/search, без ключа)
Connection:     appsettings.json → ConnectionStrings:DefaultConnection
```

---

## 📝 Соглашения по именованию

```
Файлы:          PascalCase.xaml, PascalCase.cs
Классы:         PascalCase
Методы:         PascalCase, async методы с суффиксом Async
Поля:           _camelCase (private)
Свойства:       PascalCase
XAML x:Key:     PascalCase (PrimaryButton, CardBackground)
XAML x:Name:    camelCase (searchBox, filesList)
```