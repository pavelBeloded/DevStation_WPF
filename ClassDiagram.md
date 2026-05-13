# Диаграмма классов — DevStation

## Обозначения
- `+` — public (открытый)
- `-` — private (закрытый)
- `#` — protected (защищённый)
- *Курсив* — абстрактный класс / интерфейс

**Виды связей (для схемы):**
- **Обобщение (наследование)** — сплошная линия, незакрашенный треугольник в сторону родителя
- **Реализация (интерфейс)** — штриховая линия, незакрашенный треугольник в сторону интерфейса
- **Ассоциация** — сплошная линия без стрелки (или с открытой стрелкой и именем роли)
- **Агрегация** — сплошная линия, незакрашенный ромб со стороны владельца
- **Композиция** — сплошная линия, закрашенный ромб со стороны владельца
- **Зависимость** — штриховая линия с открытой стрелкой

---

## СЛОЙ МОДЕЛЕЙ (Data Layer)

---

### `<<enumeration>>` UserRole

**Литералы:**
- `User`
- `Admin`

**Связи:** используется как тип атрибута `Role` в классе `User`

---

### `<<enumeration>>` SnippetStatus

**Литералы:**
- `Draft`
- `PendingReview`
- `Published`
- `Rejected`

**Связи:** используется как тип атрибута `Status` в классе `Snippet`

---

### `<<enumeration>>` ReviewStatus

**Литералы:**
- `Pending`
- `Approved`
- `Rejected`

**Связи:** используется как тип атрибута `Status` в классе `SnippetReview`

---

### `User`

**Атрибуты:**
```
+ Id : int
+ Username : string
+ Email : string
+ PasswordHash : string
+ Role : UserRole = UserRole.User
+ DisplayName : string?
+ CreatedAt : DateTime
+ LastLoginAt : DateTime?
+ CreatedSnippets : ICollection<Snippet>
+ UserSnippets : ICollection<UserSnippet>
+ Folders : ICollection<Folder>
+ Reviews : ICollection<SnippetReview>
```

**Связи:**
- **Ассоциация (1 → *)** → `Snippet` (CreatedSnippets — пользователь создаёт сниппеты)
- **Ассоциация (1 → *)** → `UserSnippet` (UserSnippets — библиотека пользователя)
- **Ассоциация (1 → *)** → `Folder` (Folders — папки пользователя)
- **Ассоциация (1 → *)** → `SnippetReview` (Reviews — рецензии пользователя)
- **Зависимость** → `UserRole` (использует enum)

---

### `Snippet`

**Атрибуты:**
```
+ Id : int
+ CreatorId : int
+ Creator : User
+ Title : string
+ Code : string
+ Language : string?
+ Description : string?
+ Tags : string?
+ ViewCount : int = 0
+ Status : SnippetStatus = Draft
+ IsPublic : bool = false
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
+ UserSnippets : ICollection<UserSnippet>
+ Reviews : ICollection<SnippetReview>
```

**Связи:**
- **Ассоциация (* → 1)** → `User` (Creator — FK)
- **Ассоциация (1 → *)** → `UserSnippet` (UserSnippets)
- **Ассоциация (1 → *)** → `SnippetReview` (Reviews)
- **Зависимость** → `SnippetStatus`

---

### `UserSnippet`

> Связующая сущность (many-to-many между User и Snippet)

**Атрибуты:**
```
+ UserId : int
+ User : User
+ SnippetId : int
+ Snippet : Snippet
+ FolderId : int?
+ Folder : Folder?
+ IsFavorite : bool = false
+ AddedAt : DateTime
```

**Связи:**
- **Ассоциация (* → 1)** → `User` (FK UserId)
- **Ассоциация (* → 1)** → `Snippet` (FK SnippetId)
- **Ассоциация (* → 0..1)** → `Folder` (FK FolderId, необязательный)

---

### `Folder`

**Атрибуты:**
```
+ Id : int
+ UserId : int
+ User : User
+ Name : string
+ ParentFolderId : int?
+ ParentFolder : Folder?
+ ChildFolders : ICollection<Folder>
+ Color : string?
+ CreatedAt : DateTime
+ UserSnippets : ICollection<UserSnippet>
```

**Связи:**
- **Ассоциация (* → 1)** → `User` (FK UserId)
- **Ассоциация (самоссылка, 0..1 → *)** → `Folder` (ParentFolder/ChildFolders — иерархия)
- **Ассоциация (1 → *)** → `UserSnippet`

---

### `SnippetReview`

**Атрибуты:**
```
+ Id : int
+ SnippetId : int
+ Snippet : Snippet
+ ReviewerId : int
+ Reviewer : User
+ Status : ReviewStatus = Pending
+ ReviewComment : string?
+ CreatedAt : DateTime
+ ReviewedAt : DateTime?
```

**Связи:**
- **Ассоциация (* → 1)** → `Snippet` (FK SnippetId)
- **Ассоциация (* → 1)** → `User` (FK ReviewerId — рецензент)
- **Зависимость** → `ReviewStatus`

---

### `MdnCache`

> Кэш результатов поиска MDN

**Атрибуты:**
```
+ Id : int
+ SearchTerm : string
+ Title : string
+ Summary : string?
+ Category : string?
+ Url : string?
+ CachedAt : DateTime
```

**Связи:** нет зависимостей от других сущностей

---

### `MdnSearchResult` : `ViewModelBase`

**Атрибуты:**
```
+ IsSelected : bool
+ Title : string
+ Url : string?
+ Summary : string?
+ Category : string?
+ IsFromCache : bool
+ FullUrl : string  [get]
+ Slug : string?    [get]
+ BreadcrumbText : string [get]
+ HasBreadcrumb : bool    [get]
+ HasCodeHint : bool      [get]
```

**Связи:**
- **Обобщение** → `ViewModelBase`

---

### `MdnDetails`

> DTO — детальная информация о статье MDN

**Атрибуты:**
```
+ Title : string
+ Url : string?
+ Summary : string?
+ Category : string?
+ Slug : string?
+ Syntax : string?
+ Description : string?
+ Examples : string?
```

**Связи:** нет (чистый DTO)

---

### `UserStats`

> DTO — статистика пользователя

**Атрибуты:**
```
+ TotalSnippets : int
+ InstalledSnippets : int
+ PublishedSnippets : int
+ PendingSnippets : int
```

**Связи:** нет (чистый DTO)

---

## СЛОЙ СЕРВИСОВ (Business Logic Layer)

---

### `<<interface>>` IAuthService

**Методы:**
```
+ LoginAsync(username : string, password : string) : Task<User?>
+ RegisterAsync(username : string, email : string, password : string, role : UserRole) : Task<User>
+ LogoutAsync() : Task
+ TryRestoreSessionAsync() : Task<bool>
```

**Связи:**
- **Реализация** ← `AuthService`
- **Зависимость** → `User`, `UserRole`

---

### `<<interface>>` ICurrentUserService

**Атрибуты:**
```
+ CurrentUser : User?   [get]
+ IsAuthenticated : bool [get]
+ IsAdmin : bool         [get]
```

**Методы:**
```
+ SetUser(user : User?) : void
```

**Связи:**
- **Реализация** ← `CurrentUserService`
- **Зависимость** → `User`

---

### `<<interface>>` ISnippetService

**Методы:**
```
+ GetPublicSnippetsAsync(searchQuery : string?, language : string?) : Task<IEnumerable<Snippet>>
+ GetPendingReviewSnippetsAsync() : Task<IEnumerable<Snippet>>
+ GetUserLibraryAsync(userId : int, searchQuery : string?, language : string?, filter : string?) : Task<IEnumerable<UserSnippet>>
+ SearchUserLibraryAsync(userId : int, query : string) : Task<IEnumerable<UserSnippet>>
+ GetByIdAsync(snippetId : int) : Task<Snippet?>
+ CreateSnippetAsync(creatorId : int, title : string, code : string, language : string?, description : string?, tags : string?) : Task<Snippet>
+ UpdateSnippetAsync(snippetId : int, title : string, code : string, language : string?, description : string?, tags : string?) : Task<Snippet?>
+ DeleteSnippetAsync(snippetId : int) : Task<bool>
+ InstallSnippetAsync(userId : int, snippetId : int) : Task<bool>
+ UninstallSnippetAsync(userId : int, snippetId : int) : Task
+ IsInstalledAsync(userId : int, snippetId : int) : Task<bool>
+ ToggleFavoriteAsync(userId : int, snippetId : int) : Task<bool>
+ GetUserSnippetCountAsync(userId : int) : Task<int>
+ GetUserAuthoredCountAsync(userId : int) : Task<int>
+ GetFavoritesCountAsync(userId : int) : Task<int>
+ GetRecentUserSnippetsAsync(userId : int, count : int) : Task<List<UserSnippet>>
+ SubmitForReviewAsync(snippetId : int) : Task
+ ReviewSnippetAsync(snippetId : int, reviewerId : int, status : ReviewStatus, comment : string?) : Task
+ IncrementViewCountAsync(snippetId : int) : Task
```

**Связи:**
- **Реализация** ← `SnippetService`
- **Зависимость** → `Snippet`, `UserSnippet`, `ReviewStatus`

---

### `<<interface>>` IMdnSearchService

**Методы:**
```
+ SearchAsync(query : string) : Task<List<MdnSearchResult>>
+ GetDetailsAsync(url : string) : Task<MdnDetails>
+ GetTotalSearchCountAsync() : Task<int>
```

**Связи:**
- **Реализация** ← `MdnSearchService`
- **Зависимость** → `MdnSearchResult`, `MdnDetails`

---

### `<<interface>>` IAccountService

**Методы:**
```
+ UpdateUsernameAsync(userId : int, newUsername : string) : Task<bool>
+ UpdatePasswordAsync(userId : int, currentPassword : string, newPassword : string) : Task<bool>
+ GetUserStatsAsync(userId : int) : Task<UserStats>
+ GetAllUsersAsync() : Task<List<User>>
+ PromoteToAdminAsync(targetUserId : int, requestingUserId : int) : Task<bool>
+ DemoteToUserAsync(targetUserId : int, requestingUserId : int) : Task<bool>
```

**Связи:**
- **Реализация** ← `AccountService`
- **Зависимость** → `UserStats`, `User`

---

### `AuthService` : `IAuthService`

**Атрибуты:**
```
- _db : DevStationDbContext
- _currentUserService : ICurrentUserService
- _sessionPath : string
```

**Методы:**
```
+ LoginAsync(username : string, password : string) : Task<User?>
+ RegisterAsync(username : string, email : string, password : string, role : UserRole) : Task<User>
+ LogoutAsync() : Task
+ TryRestoreSessionAsync() : Task<bool>
```

**Связи:**
- **Реализация** → `IAuthService`
- **Зависимость (композиция)** → `DevStationDbContext`
- **Ассоциация** → `ICurrentUserService`

---

### `CurrentUserService` : `ICurrentUserService`

**Атрибуты:**
```
- _currentUser : User?
```

**Методы:**
```
+ SetUser(user : User?) : void
```

**Атрибуты-свойства:**
```
+ CurrentUser : User?
+ IsAuthenticated : bool
+ IsAdmin : bool
```

**Связи:**
- **Реализация** → `ICurrentUserService`

---

### `SnippetService` : `ISnippetService`

**Атрибуты:**
```
- _db : DevStationDbContext
```

**Методы:** (см. интерфейс ISnippetService — все реализованы)

**Связи:**
- **Реализация** → `ISnippetService`
- **Зависимость (композиция)** → `DevStationDbContext`

---

### `MdnSearchService` : `IMdnSearchService`

**Атрибуты:**
```
- _db : DevStationDbContext
- _http : IHttpClientFactory
```

**Методы:**
```
+ SearchAsync(query : string) : Task<List<MdnSearchResult>>
+ GetDetailsAsync(url : string) : Task<MdnDetails>
+ GetTotalSearchCountAsync() : Task<int>
- StripHtml(html : string?) : string
- ExtractSlug(url : string?) : string
- ExtractCategory(url : string) : string
```

**Связи:**
- **Реализация** → `IMdnSearchService`
- **Зависимость (композиция)** → `DevStationDbContext`
- **Зависимость** → `IHttpClientFactory`
- **Зависимость** → `MdnCache` (кэширует результаты)

---

### `AccountService` : `IAccountService`

**Атрибуты:**
```
- _db : DevStationDbContext
```

**Методы:** (см. интерфейс IAccountService — все реализованы)

**Связи:**
- **Реализация** → `IAccountService`
- **Зависимость (композиция)** → `DevStationDbContext`

---

### `<<static>>` ImageOptimizerService

**Методы:**
```
+ OptimizeAsync(filePath : string, progress : IProgress<int>?) : Task<byte[]>  [static]
- OptimizePng(image : Image, progress : IProgress<int>?) : Task<byte[]>        [static]
- OptimizeJpeg(image : Image, progress : IProgress<int>?) : Task<byte[]>       [static]
- OptimizeWebP(image : Image, progress : IProgress<int>?) : Task<byte[]>       [static]
- OptimizeSvg(filePath : string, progress : IProgress<int>?) : Task<byte[]>    [static]
```

**Связи:**
- нет зависимостей от других классов проекта (использует SixLabors.ImageSharp)

---

## СЛОЙ ПРЕДСТАВЛЕНИЯ (Presentation Layer)

---

### `<<abstract>>` ViewModelBase : INotifyPropertyChanged

**Методы:**
```
# SetProperty<T>(field : T, value : T, propertyName : string?) : bool
# OnPropertyChanged(propertyName : string?) : void
```

**Связи:**
- **Реализация** → `INotifyPropertyChanged`
- **Обобщение** ← все конкретные ViewModel-классы

---

### `RelayCommand` : ICommand

**Атрибуты:**
```
- _execute : Action<object?>
- _canExecute : Func<object?, bool>?
```

**Методы:**
```
+ RelayCommand(execute : Action<object?>, canExecute : Func<object?, bool>?)
+ RelayCommand(execute : Action, canExecute : Func<bool>?)
+ CanExecute(parameter : object?) : bool
+ Execute(parameter : object?) : void
+ RaiseCanExecuteChanged() : void
```

**Связи:**
- **Реализация** → `ICommand`

---

### `RelayCommand<T>` : ICommand

**Атрибуты:**
```
- _execute : Action<T?>
- _canExecute : Func<T?, bool>?
```

**Методы:**
```
+ RelayCommand(execute : Action<T?>, canExecute : Func<T?, bool>?)
+ CanExecute(parameter : object?) : bool
+ Execute(parameter : object?) : void
```

**Связи:**
- **Реализация** → `ICommand`

---

### `LoginViewModel` : ViewModelBase

**Атрибуты:**
```
- _authService : IAuthService
- _mainWindowFactory : Func<MainWindow>
+ Username : string
+ Password : string
+ NewUsername : string
+ Email : string
+ RegisterPassword : string
+ ConfirmPassword : string
+ ErrorMessage : string
+ IsLoading : bool
+ IsLoginMode : bool
+ LoginCommand : RelayCommand
+ RegisterCommand : RelayCommand
+ SwitchToLoginCommand : RelayCommand
+ SwitchToRegisterCommand : RelayCommand
+ ForgotPasswordCommand : RelayCommand
```

**Методы:**
```
- CanLogin() : bool
- CanRegister() : bool
- ExecuteLoginAsync() : Task
- ExecuteRegisterAsync() : Task
- OpenMainWindow() : void
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `IAuthService`
- **Агрегация** → `RelayCommand` (5 команд)

---

### `MainWindowViewModel` : ViewModelBase

**Атрибуты:**
```
- _currentUserService : ICurrentUserService
- _authService : IAuthService
- _snippetService : ISnippetService
- _mdnSearchService : IMdnSearchService
- _accountService : IAccountService
- _loginWindowFactory : Func<LoginWindow>
- _pageCache : Dictionary<string, ViewModelBase>
+ CurrentUser : User?               [get]
+ DisplayName : string              [get]
+ IsAdmin : bool                    [get]
+ CurrentPage : ViewModelBase       [get]
+ CurrentPageKey : string           [get]
+ SearchQuery : string
+ SearchResults : ObservableCollection<SearchResultItem>
+ MdnResults : ObservableCollection<MdnSearchResult>
+ IsMdnLoading : bool
+ HasDollarQuery : bool             [get]
+ HasQuestionQuery : bool           [get]
+ IsSearchOpen : bool               [get]
+ IsMdnSearchOpen : bool            [get]
+ IsAnySearchOpen : bool            [get]
+ LogoutCommand : RelayCommand
+ NavigateCommand : RelayCommand<string>
+ UseFirstResultCommand : RelayCommand
+ UseResultCommand : RelayCommand<SearchResultItem>
+ OpenMdnResultCommand : RelayCommand<MdnSearchResult>
```

**Методы:**
```
+ SelectNextResult() : void
+ SelectPreviousResult() : void
+ SelectNextMdnResult() : void
+ SelectPreviousMdnResult() : void
+ OpenSelectedMdnResult() : void
+ NavigateToVm(vm : ViewModelBase) : void
- NavigateTo(pageKey : string?) : void
- GetOrCreatePage(key : string) : ViewModelBase
- SearchAsync(query : string) : Task
- SearchMdnWithDebounceAsync(query : string) : Task
- ExecuteLogoutAsync() : Task
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `ICurrentUserService`, `IAuthService`, `ISnippetService`, `IMdnSearchService`, `IAccountService`
- **Агрегация** → `SearchResultItem` (коллекция)
- **Агрегация** → `MdnSearchResult` (коллекция)
- **Композиция** → все страничные VM через кэш (создаёт и хранит)

---

### `SearchResultItem` : ViewModelBase

**Атрибуты:**
```
+ UserSnippet : UserSnippet  [get]
+ IsSelected : bool
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Ассоциация** → `UserSnippet`

---

### `HomeViewModel` : ViewModelBase

**Атрибуты:**
```
- _snippetService : ISnippetService
- _mdnService : IMdnSearchService
- _currentUserService : ICurrentUserService
- _navigateByKey : Action<string>
- _navigateTo : Action<ViewModelBase>
+ SnippetCount : int
+ AuthoredCount : int
+ FavoritesCount : int
+ MdnCount : int
+ LastSnippetTitle : string
+ LastSnippetCodePreview : string
+ HasLastSnippet : bool
+ RecentSnippets : ObservableCollection<RecentSnippetItem>
+ HasRecentSnippets : bool   [get]
+ UserName : string          [get]
+ UserInitials : string      [get]
+ CreateSnippetCommand : RelayCommand
+ NavigateCommand : RelayCommand<string>
```

**Методы:**
```
+ LoadDataAsync() : Task
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `ISnippetService`, `IMdnSearchService`, `ICurrentUserService`
- **Агрегация** → `RecentSnippetItem` (коллекция)

---

### `RecentSnippetItem`

**Атрибуты:**
```
+ Title : string        [get]
+ Badge : string        [get]
+ TimeAgo : string      [get]
+ IsFavorite : bool     [get]
+ BadgeBrush : SolidColorBrush [get]
```

**Методы:**
```
- GetBadge(lang : string?) : string         [static]
- GetBadgeBrush(lang : string?) : SolidColorBrush [static]
- GetTimeAgo(dt : DateTime) : string        [static]
```

**Связи:** нет зависимостей от других классов проекта

---

### `SnippetsViewModel` : ViewModelBase

**Атрибуты:**
```
- _snippetService : ISnippetService
- _currentUserService : ICurrentUserService
- _navigateTo : Action<ViewModelBase>
+ IsAdmin : bool                  [get]
+ IsMyLibraryTab : bool           [get]
+ PublicSnippets : ObservableCollection<SnippetCardItem>
+ MyLibrary : ObservableCollection<UserSnippet>
+ PendingReviews : ObservableCollection<Snippet>
+ SearchQuery : string
+ SelectedLanguage : string
+ SelectedFilter : string
+ IsGlobalTab : bool
+ IsLoading : bool
+ TotalSnippetsCount : int
+ InstalledCount : int
+ AuthoredCount : int
+ PendingCount : string           [get]
+ Languages : List<string>        [get]
+ FilterOptions : List<string>    [get]
+ ShowGlobalTabCommand : RelayCommand
+ ShowMyLibraryTabCommand : RelayCommand
+ SelectFilterCommand : RelayCommand<string>
+ InstallCommand : RelayCommand<SnippetCardItem>
+ UseSnippetCommand : RelayCommand<UserSnippet>
+ OpenDetailCommand : RelayCommand<SnippetCardItem>
+ OpenLibraryDetailCommand : RelayCommand<UserSnippet>
+ OpenPendingDetailCommand : RelayCommand<Snippet>
+ AdminEditPendingCommand : RelayCommand<Snippet>
+ CreateSnippetCommand : RelayCommand
+ ApproveCommand : RelayCommand<Snippet>
+ RejectCommand : RelayCommand<Snippet>
```

**Методы:**
```
+ LoadDataAsync() : Task
- OpenDetailAsync(snippet : Snippet, isInstalled : bool, isFavorite : bool) : Task
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `ISnippetService`, `ICurrentUserService`
- **Агрегация** → `SnippetCardItem` (коллекция публичных сниппетов)
- **Агрегация** → `UserSnippet` (коллекция библиотеки)
- **Агрегация** → `Snippet` (коллекция ожидающих проверки)

---

### `SnippetCardItem` : ViewModelBase

**Атрибуты:**
```
+ Snippet : Snippet              [get]
+ IsInstalled : bool
+ TagItems : IEnumerable<string> [get]
+ HasTags : bool                 [get]
+ AuthorName : string            [get]
+ FileName : string              [get]
+ FileExtension : string         [get]
+ InstallCount : int             [get]
```

**Методы:**
```
- LanguageToExt(lang : string?) : string [static]
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Ассоциация** → `Snippet`

---

### `SnippetDetailViewModel` : ViewModelBase

**Атрибуты:**
```
- _snippetService : ISnippetService
- _currentUserService : ICurrentUserService
- _navigateBack : Action
- _navigateTo : Action<ViewModelBase>
+ Snippet : Snippet              [get]
+ IsInstalled : bool
+ IsFavorite : bool
+ CanFavorite : bool             [get]
+ IsOwner : bool                 [get]
+ HasDescription : bool          [get]
+ HasTags : bool                 [get]
+ HasLanguage : bool             [get]
+ CanInstall : bool              [get]
+ CanUse : bool                  [get]
+ CanEditAndSubmit : bool        [get]
+ AwaitingReview : bool          [get]
+ WasRejected : bool             [get]
+ WasPublished : bool            [get]
+ CanUninstall : bool            [get]
+ CanAdminEdit : bool            [get]
+ CanAdminDelete : bool          [get]
+ AuthorName : string            [get]
+ LineCount : int                [get]
+ InstallCount : int             [get]
+ CreatedAtText : string         [get]
+ StatusText : string            [get]
+ FileName : string              [get]
+ FileExtension : string         [get]
+ CodeSizeText : string          [get]
+ TagItems : IEnumerable<string> [get]
+ RejectionComment : string?     [get]
+ NavigateBackCommand : RelayCommand
+ InstallCommand : RelayCommand
+ UseSnippetCommand : RelayCommand
+ UninstallCommand : RelayCommand
+ ToggleFavoriteCommand : RelayCommand
+ EditCommand : RelayCommand
+ SubmitForReviewCommand : RelayCommand
+ DeleteCommand : RelayCommand
+ AdminDeleteCommand : RelayCommand
```

**Методы:**
```
- RefreshComputedProps() : void
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `ISnippetService`, `ICurrentUserService`
- **Ассоциация** → `Snippet`

---

### `CreateSnippetViewModel` : ViewModelBase

**Атрибуты:**
```
- _editingSnippet : Snippet?
- _snippetService : ISnippetService
- _currentUserService : ICurrentUserService
- _navigateBack : Action
- _navigateTo : Action<ViewModelBase>
+ IsEditMode : bool              [get]
+ PageTitle : string             [get]
+ Title : string
+ Description : string
+ Language : string = "JavaScript"
+ Tags : string
+ Code : string
+ IsSaving : bool
+ ErrorMessage : string
+ HasError : bool                [get]
+ CanSave : bool                 [get]
+ HasTags : bool                 [get]
+ TagItems : IEnumerable<string> [get]
+ FileName : string              [get]
+ FileExtension : string         [get]
+ LineCount : int                [get]
+ CodeSizeText : string          [get]
+ Languages : List<string>       [get]
+ NavigateBackCommand : RelayCommand
+ SaveDraftCommand : RelayCommand
+ SaveAndSubmitCommand : RelayCommand
```

**Методы:**
```
- SaveAsync(submitForReview : bool) : Task
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `ISnippetService`, `ICurrentUserService`
- **Ассоциация** → `Snippet` (редактируемый сниппет, опциональный)

---

### `ConverterViewModel` : ViewModelBase

**Атрибуты:**
```
+ SvgCode : string
+ ReactCode : string
+ IsEmpty : bool
+ StatusText : string
+ CopyCommand : RelayCommand
+ DownloadCommand : RelayCommand
+ ClearCommand : RelayCommand
```

**Методы:**
```
- RunConversion() : void
- SvgToJsx(svgCode : string) : string                                           [static]
- AppendElement(sb : StringBuilder, el : XElement, depth : int, isRoot : bool) : void [static]
- ToJsxName(name : string) : string                                             [static]
- ToJsxValue(jsxName : string, value : string, isRoot : bool) : string          [static]
- ExecuteCopy() : void
- ExecuteDownload() : void
- ExecuteClear() : void
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- нет зависимостей от других классов проекта

---

### `MdnSearchViewModel` : ViewModelBase

**Атрибуты:**
```
- _mdnService : IMdnSearchService
+ SearchQuery : string
+ SearchResults : ObservableCollection<MdnSearchResult>
+ SelectedResult : MdnSearchResult?
+ SelectedDetails : MdnDetails?
+ IsLoading : bool
+ IsLoadingDetails : bool
+ IsOffline : bool
+ ErrorMessage : string
+ HasResults : bool              [get]
+ HasNoResults : bool            [get]
+ IsResultSelected : bool        [get]
+ HasSyntax : bool               [get]
+ HasDescription : bool          [get]
+ HasExamples : bool             [get]
+ FullMdnUrl : string?           [get]
+ ClearCommand : RelayCommand
+ CopyUrlCommand : RelayCommand
+ CopySummaryCommand : RelayCommand
+ CopyExamplesCommand : RelayCommand
+ OpenInBrowserCommand : RelayCommand
+ SelectResultCommand : RelayCommand<MdnSearchResult>
```

**Методы:**
```
+ LoadExternalResults(query : string, results : List<MdnSearchResult>, selected : MdnSearchResult?) : void
- LoadDetailsAsync(result : MdnSearchResult) : Task
- SearchWithDebounceAsync(query : string) : Task
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Зависимость** → `IMdnSearchService`
- **Агрегация** → `MdnSearchResult` (коллекция)
- **Ассоциация** → `MdnDetails`

---

### `ImageOptimizerViewModel` : ViewModelBase

**Атрибуты:**
```
+ Files : ObservableCollection<ImageFileItem>
+ RemoveFileCommand : RelayCommand<ImageFileItem>
+ DownloadFileCommand : RelayCommand<ImageFileItem>
+ DeleteAllCommand : RelayCommand
+ DownloadAllCommand : RelayCommand
```

**Методы:**
```
+ AddFilesAsync(paths : IEnumerable<string>) : Task
- OptimizeAsync(item : ImageFileItem) : Task [static]
- LoadThumbnail(path : string) : BitmapImage? [static]
- ExtToMime(ext : string) : string            [static]
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Агрегация** → `ImageFileItem` (коллекция)
- **Зависимость** → `ImageOptimizerService` (вызывает статический метод)

---

### `ImageFileItem` : ViewModelBase

**Атрибуты:**
```
+ FileName : string          [init]
+ FilePath : string          [init]
+ MimeType : string          [init]
+ OriginalSize : long        [init]
+ Thumbnail : BitmapImage?   [init]
+ OptimizedSize : long
+ Status : OptimizationStatus
+ Progress : int
+ OptimizedBytes : byte[]?
+ IsCompleted : bool         [get]
+ IsOptimizing : bool        [get]
+ IsQueued : bool            [get]
+ HasThumbnail : bool        [get]
+ OriginalSizeText : string  [get]
+ OptimizedSizeText : string [get]
+ SavingsText : string       [get]
```

**Связи:**
- **Обобщение** → `ViewModelBase`

---

### `MockGeneratorViewModel` : ViewModelBase

**Атрибуты:**
```
+ EntityName : string = "User_Profile_Schema"
+ RecordCount : int = 50
+ IsJsonFormat : bool = true
+ OutputFileName : string        [get]
+ Fields : ObservableCollection<MockFieldItem>
+ OutputText : string
+ IsReady : bool
+ OutputLines : int
+ OutputSize : string
+ SelectJsonCommand : RelayCommand
+ SelectSqlCommand : RelayCommand
+ AddFieldCommand : RelayCommand
+ DeleteFieldCommand : RelayCommand<MockFieldItem>
+ GenerateCommand : RelayCommand
+ RegenerateCommand : RelayCommand
+ CopyCommand : RelayCommand
+ DownloadCommand : RelayCommand
```

**Методы:**
```
- AddField() : void
- Generate() : void
- BuildJson(faker : Faker) : string
- BuildSql(faker : Faker) : string
- GenerateValue(faker : Faker, typeKey : string, index : int) : object [static]
- GetSqlType(typeKey : string) : string                                [static]
- ToSqlLiteral(val : object) : string                                  [static]
- UpdateStats() : void
- Download() : void
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Агрегация** → `MockFieldItem` (коллекция)

---

### `MockFieldItem` : ViewModelBase

**Атрибуты:**
```
+ Name : string = "field"
+ TypeKey : string = "fullName"
+ TypeLabel : string                            [get]
+ TypeOptions : IReadOnlyList<FieldTypeOption>  [static, get]
```

**Связи:**
- **Обобщение** → `ViewModelBase`
- **Ассоциация** → `FieldTypeOption` (список опций)

---

### `FieldTypeOption`

**Атрибуты:**
```
+ Key : string    [init]
+ Label : string  [init]
```

**Связи:** нет

---

### `DocumentationViewModel` : ViewModelBase

**Атрибуты:**
```
+ ActiveSection : string = "getting-started"
+ ScrollToCommand : ICommand
+ ScrollToSection : Action<string>?
```

**Связи:**
- **Обобщение** → `ViewModelBase`

---

## СВОДНАЯ ТАБЛИЦА СВЯЗЕЙ

| От | Вид связи | К | Описание |
|---|---|---|---|
| `ViewModelBase` | Реализация (штрих + треугольник) | `INotifyPropertyChanged` | |
| `RelayCommand` | Реализация | `ICommand` | |
| `RelayCommand<T>` | Реализация | `ICommand` | |
| `LoginViewModel` | Обобщение (сплошная + треугольник) | `ViewModelBase` | |
| `MainWindowViewModel` | Обобщение | `ViewModelBase` | |
| `HomeViewModel` | Обобщение | `ViewModelBase` | |
| `SnippetsViewModel` | Обобщение | `ViewModelBase` | |
| `SnippetDetailViewModel` | Обобщение | `ViewModelBase` | |
| `CreateSnippetViewModel` | Обобщение | `ViewModelBase` | |
| `ConverterViewModel` | Обобщение | `ViewModelBase` | |
| `MdnSearchViewModel` | Обобщение | `ViewModelBase` | |
| `ImageOptimizerViewModel` | Обобщение | `ViewModelBase` | |
| `MockGeneratorViewModel` | Обобщение | `ViewModelBase` | |
| `DocumentationViewModel` | Обобщение | `ViewModelBase` | |
| `SearchResultItem` | Обобщение | `ViewModelBase` | |
| `SnippetCardItem` | Обобщение | `ViewModelBase` | |
| `ImageFileItem` | Обобщение | `ViewModelBase` | |
| `MockFieldItem` | Обобщение | `ViewModelBase` | |
| `MdnSearchResult` | Обобщение | `ViewModelBase` | |
| `AuthService` | Реализация | `IAuthService` | |
| `CurrentUserService` | Реализация | `ICurrentUserService` | |
| `SnippetService` | Реализация | `ISnippetService` | |
| `MdnSearchService` | Реализация | `IMdnSearchService` | |
| `AccountService` | Реализация | `IAccountService` | |
| `AuthService` | Композиция (закр. ромб) | `DevStationDbContext` | хранит ссылку, не создаёт |
| `SnippetService` | Композиция | `DevStationDbContext` | |
| `MdnSearchService` | Композиция | `DevStationDbContext` | |
| `AccountService` | Композиция | `DevStationDbContext` | |
| `AuthService` | Ассоциация | `ICurrentUserService` | вызывает SetUser |
| `MdnSearchService` | Зависимость (штрих + стрелка) | `IHttpClientFactory` | для HTTP запросов к MDN |
| `MdnSearchService` | Ассоциация | `MdnCache` | кэширует через DbContext |
| `LoginViewModel` | Зависимость | `IAuthService` | |
| `MainWindowViewModel` | Зависимость | `ICurrentUserService` | |
| `MainWindowViewModel` | Зависимость | `IAuthService` | |
| `MainWindowViewModel` | Зависимость | `ISnippetService` | |
| `MainWindowViewModel` | Зависимость | `IMdnSearchService` | |
| `MainWindowViewModel` | Зависимость | `IAccountService` | |
| `MainWindowViewModel` | Агрегация (незакр. ромб) | `SearchResultItem` | ObservableCollection |
| `MainWindowViewModel` | Агрегация | `MdnSearchResult` | ObservableCollection |
| `HomeViewModel` | Зависимость | `ISnippetService` | |
| `HomeViewModel` | Зависимость | `IMdnSearchService` | |
| `HomeViewModel` | Зависимость | `ICurrentUserService` | |
| `HomeViewModel` | Агрегация | `RecentSnippetItem` | ObservableCollection |
| `SnippetsViewModel` | Зависимость | `ISnippetService` | |
| `SnippetsViewModel` | Зависимость | `ICurrentUserService` | |
| `SnippetsViewModel` | Агрегация | `SnippetCardItem` | PublicSnippets |
| `SnippetDetailViewModel` | Зависимость | `ISnippetService` | |
| `SnippetDetailViewModel` | Зависимость | `ICurrentUserService` | |
| `SnippetDetailViewModel` | Ассоциация | `Snippet` | отображаемый сниппет |
| `CreateSnippetViewModel` | Зависимость | `ISnippetService` | |
| `CreateSnippetViewModel` | Зависимость | `ICurrentUserService` | |
| `CreateSnippetViewModel` | Ассоциация (0..1) | `Snippet` | редактируемый сниппет |
| `MdnSearchViewModel` | Зависимость | `IMdnSearchService` | |
| `MdnSearchViewModel` | Агрегация | `MdnSearchResult` | ObservableCollection |
| `MdnSearchViewModel` | Ассоциация | `MdnDetails` | деталь выбранного результата |
| `ImageOptimizerViewModel` | Агрегация | `ImageFileItem` | ObservableCollection |
| `ImageOptimizerViewModel` | Зависимость | `ImageOptimizerService` | статический вызов |
| `MockGeneratorViewModel` | Агрегация | `MockFieldItem` | ObservableCollection |
| `MockFieldItem` | Ассоциация | `FieldTypeOption` | список типов (static) |
| `SnippetCardItem` | Ассоциация | `Snippet` | |
| `SearchResultItem` | Ассоциация | `UserSnippet` | |
| `User` | Ассоциация (1→*) | `Snippet` | CreatedSnippets |
| `User` | Ассоциация (1→*) | `UserSnippet` | библиотека |
| `User` | Ассоциация (1→*) | `Folder` | папки пользователя |
| `User` | Ассоциация (1→*) | `SnippetReview` | написанные рецензии |
| `Snippet` | Ассоциация (*→1) | `User` | Creator (FK) |
| `Snippet` | Ассоциация (1→*) | `UserSnippet` | |
| `Snippet` | Ассоциация (1→*) | `SnippetReview` | |
| `UserSnippet` | Ассоциация (*→1) | `User` | FK UserId |
| `UserSnippet` | Ассоциация (*→1) | `Snippet` | FK SnippetId |
| `UserSnippet` | Ассоциация (*→0..1) | `Folder` | FK FolderId |
| `Folder` | Ассоциация (*→1) | `User` | FK UserId |
| `Folder` | Ассоциация (самоссылка) | `Folder` | ParentFolder / ChildFolders |
| `Folder` | Ассоциация (1→*) | `UserSnippet` | |
| `SnippetReview` | Ассоциация (*→1) | `Snippet` | FK SnippetId |
| `SnippetReview` | Ассоциация (*→1) | `User` | FK ReviewerId |
| `User` | Зависимость | `UserRole` | тип атрибута Role |
| `Snippet` | Зависимость | `SnippetStatus` | тип атрибута Status |
| `SnippetReview` | Зависимость | `ReviewStatus` | тип атрибута Status |
