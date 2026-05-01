# PRD — DeepL Auto-Translation Integration

## ASP.NET Core Blog System

## deepl api key = ""

## 1. Overview

Integrate DeepL's translation API into an existing ASP.NET Core Web API blog system to automatically translate blog post content (title + body) between **English** and **Arabic** when a post is created or updated. Translated content is stored in the database alongside the original to avoid re-translating on every request.

---

## 2. Goals

- Auto-translate blog post title and content to both `en` and `ar` on save
- Serve the correct language version based on a `?lang=` query parameter
- Never call the translation API on read — only on write
- Keep implementation clean, injectable, and testable

---

## 3. Tech Stack

| Layer       | Technology                         |
| ----------- | ---------------------------------- |
| Backend     | ASP.NET Core Web API (.NET 8)      |
| ORM         | Entity Framework Core              |
| Translation | DeepL API (Free Tier)              |
| SDK         | `DeepL.net` NuGet package          |
| Database    | SQL Server / PostgreSQL (existing) |

---

## 4. Requirements

### 4.1 Install NuGet Package

```bash
dotnet add package DeepL.net
```

---

### 4.2 Add API Key to Configuration

In `appsettings.json`:

```json
{
  "DeepL": {
    "ApiKey": "YOUR_DEEPL_API_KEY_HERE:fx"
  }
}
```

> ⚠️ The free tier key always ends with `:fx` — do not remove it.

In `appsettings.Development.json` use the same key for local testing.  
Never commit the real key to Git — use User Secrets or environment variables in production.

---

### 4.3 Database — Update BlogPost Entity

Update the existing `BlogPost` entity to include translation columns:

```csharp
public class BlogPost
{
    public int Id { get; set; }

    // Original input fields (filled by the author)
    public string Title { get; set; }
    public string Content { get; set; }
    public string OriginalLanguage { get; set; } // "en" or "ar"

    // Auto-translated fields (filled by the system)
    public string TitleEn { get; set; }
    public string ContentEn { get; set; }
    public string TitleAr { get; set; }
    public string ContentAr { get; set; }

    // Metadata
    public bool IsTranslated { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

Run a migration after updating the model:

```bash
dotnet ef migrations add AddTranslationColumns
dotnet ef database update
```

---

### 4.4 Create Translation Service

Create file: `Services/TranslationService.cs`

```csharp
using DeepL;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage);
}

public class TranslationService : ITranslationService
{
    private readonly Translator _translator;

    public TranslationService(IConfiguration config)
    {
        var apiKey = config["DeepL:ApiKey"];
        _translator = new Translator(apiKey);
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage)
    {
        // targetLanguage: "AR" for Arabic, "EN-US" for English
        var result = await _translator.TranslateTextAsync(
            text,
            detectedSourceLanguageCode: null,
            targetLanguageCode: targetLanguage
        );
        return result.Text;
    }
}
```

---

### 4.5 Create Blog Service with Auto-Translate Logic

Create file: `Services/BlogService.cs`

```csharp
public class BlogService
{
    private readonly AppDbContext _db;
    private readonly ITranslationService _translator;

    public BlogService(AppDbContext db, ITranslationService translator)
    {
        _db = db;
        _translator = translator;
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        await PopulateTranslations(post);
        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync();
        return post;
    }

    public async Task<BlogPost> UpdatePostAsync(BlogPost post)
    {
        await PopulateTranslations(post);
        post.UpdatedAt = DateTime.UtcNow;
        _db.BlogPosts.Update(post);
        await _db.SaveChangesAsync();
        return post;
    }

    private async Task PopulateTranslations(BlogPost post)
    {
        if (post.OriginalLanguage == "en")
        {
            post.TitleEn   = post.Title;
            post.ContentEn = post.Content;
            post.TitleAr   = await _translator.TranslateAsync(post.Title,   "AR");
            post.ContentAr = await _translator.TranslateAsync(post.Content, "AR");
        }
        else // "ar"
        {
            post.TitleAr   = post.Title;
            post.ContentAr = post.Content;
            post.TitleEn   = await _translator.TranslateAsync(post.Title,   "EN-US");
            post.ContentEn = await _translator.TranslateAsync(post.Content, "EN-US");
        }

        post.IsTranslated = true;
    }
}
```

---

### 4.6 Register Services in Program.cs

In `Program.cs`, register both services:

```csharp
builder.Services.AddSingleton<ITranslationService, TranslationService>();
builder.Services.AddScoped<BlogService>();
```

---

### 4.7 Update Blog Controller

Update `Controllers/BlogController.cs`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class BlogController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly BlogService _blogService;

    public BlogController(AppDbContext db, BlogService blogService)
    {
        _db = db;
        _blogService = blogService;
    }

    // GET api/blog/{id}?lang=ar
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id, [FromQuery] string lang = "en")
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post == null) return NotFound();

        return Ok(new
        {
            id      = post.Id,
            title   = lang == "ar" ? post.TitleAr   : post.TitleEn,
            content = lang == "ar" ? post.ContentAr : post.ContentEn,
            lang,
            createdAt = post.CreatedAt
        });
    }

    // GET api/blog?lang=ar
    [HttpGet]
    public async Task<IActionResult> GetAllPosts([FromQuery] string lang = "en")
    {
        var posts = await _db.BlogPosts.ToListAsync();

        var result = posts.Select(post => new
        {
            id      = post.Id,
            title   = lang == "ar" ? post.TitleAr : post.TitleEn,
            content = lang == "ar" ? post.ContentAr : post.ContentEn,
            lang,
            createdAt = post.CreatedAt
        });

        return Ok(result);
    }

    // POST api/blog
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreateBlogPostDto dto)
    {
        var post = new BlogPost
        {
            Title            = dto.Title,
            Content          = dto.Content,
            OriginalLanguage = dto.OriginalLanguage ?? "en"
        };

        var created = await _blogService.CreatePostAsync(post);
        return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
    }

    // PUT api/blog/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] CreateBlogPostDto dto)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post == null) return NotFound();

        post.Title            = dto.Title;
        post.Content          = dto.Content;
        post.OriginalLanguage = dto.OriginalLanguage ?? post.OriginalLanguage;

        var updated = await _blogService.UpdatePostAsync(post);
        return Ok(updated);
    }
}
```

---

### 4.8 DTO

Create file: `DTOs/CreateBlogPostDto.cs`

```csharp
public class CreateBlogPostDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string OriginalLanguage { get; set; } // "en" or "ar"
}
```

---

## 5. API Usage Examples

### Create a post in English → auto-translates to Arabic

```http
POST /api/blog
Content-Type: application/json

{
  "title": "How to Build Great Apps",
  "content": "Building great apps requires planning, design, and execution.",
  "originalLanguage": "en"
}
```

### Create a post in Arabic → auto-translates to English

```http
POST /api/blog
Content-Type: application/json

{
  "title": "كيف تبني تطبيقات رائعة",
  "content": "بناء التطبيقات الرائعة يتطلب التخطيط والتصميم والتنفيذ.",
  "originalLanguage": "ar"
}
```

### Get post in Arabic

```http
GET /api/blog/1?lang=ar
```

### Get post in English

```http
GET /api/blog/1?lang=en
```

---

## 6. Folder Structure

```
YourProject/
├── Controllers/
│   └── BlogController.cs        ← update this
├── DTOs/
│   └── CreateBlogPostDto.cs     ← create this
├── Models/
│   └── BlogPost.cs              ← update this
├── Services/
│   ├── ITranslationService.cs   ← create this
│   ├── TranslationService.cs    ← create this
│   └── BlogService.cs           ← create this
├── Data/
│   └── AppDbContext.cs          ← no changes needed
├── appsettings.json             ← add DeepL key
└── Program.cs                   ← register services
```

---

## 7. Error Handling

Wrap translation calls to avoid crashing the entire save if DeepL fails:

```csharp
private async Task<string> SafeTranslate(string text, string targetLang)
{
    try
    {
        return await _translator.TranslateAsync(text, targetLang);
    }
    catch (Exception ex)
    {
        // Log the error
        Console.WriteLine($"Translation failed: {ex.Message}");
        // Return empty string — post still saves, just without translation
        return string.Empty;
    }
}
```

---

## 8. Important Notes for Cursor

- Do **not** translate on GET requests — only on POST and PUT
- The DeepL free key **must end with `:fx`** — validate this on startup
- Arabic target code for DeepL is `"AR"`, English is `"EN-US"`
- Store all 4 translated fields even if original language matches one of them
- Use `AddSingleton` for `TranslationService` since `Translator` is thread-safe
- Use `AddScoped` for `BlogService` since it depends on `AppDbContext`
- Run `dotnet ef migrations add AddTranslationColumns` after model update

---

## 9. Testing Checklist

- [ ] Create post in English → verify `TitleAr` and `ContentAr` are populated in DB
- [ ] Create post in Arabic → verify `TitleEn` and `ContentEn` are populated in DB
- [ ] GET post with `?lang=ar` → returns Arabic fields
- [ ] GET post with `?lang=en` → returns English fields
- [ ] Update post → verify translations are refreshed
- [ ] DeepL API key is not hardcoded anywhere in source code
