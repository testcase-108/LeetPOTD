using System.Net.Http.Json;
using System.Text.Json;
using LeetPOTD.Models;

namespace LeetPOTD.Services;

public sealed class LeetCodeService(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment) : ILeetCodeService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".cpp",
        ".c",
        ".java",
        ".py",
        ".js",
        ".ts",
        ".go",
        ".rs"
    };

    public async Task<PotdProblem> GetDailyProblemAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "https://leetcode.com/graphql",
            new
            {
                query = """
                    query questionOfToday {
                      activeDailyCodingChallengeQuestion {
                        date
                        link
                        question {
                          title
                          titleSlug
                          difficulty
                          content
                        }
                      }
                    }
                    """
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var daily = document.RootElement
            .GetProperty("data")
            .GetProperty("activeDailyCodingChallengeQuestion");

        var question = daily.GetProperty("question");
        var link = daily.GetProperty("link").GetString();
        var dateText = daily.GetProperty("date").GetString();

        return new PotdProblem
        {
            Title = question.GetProperty("title").GetString() ?? string.Empty,
            Slug = question.GetProperty("titleSlug").GetString() ?? string.Empty,
            Difficulty = question.GetProperty("difficulty").GetString(),
            Statement = question.GetProperty("content").GetString(),
            Url = string.IsNullOrWhiteSpace(link) ? null : $"https://leetcode.com{link}",
            Date = DateOnly.TryParse(dateText, out var date) ? date : null
        };
    }

    public async Task<LocalSolution?> FindLocalSolutionAsync(PotdProblem problem, CancellationToken cancellationToken = default)
    {
        var root = ResolveSolutionsRoot();

        if (!Directory.Exists(root))
        {
            return null;
        }

        var titleToken = Normalize(problem.Title);
        var slugToken = Normalize(problem.Slug);
        var problemTokens = new[] { slugToken, titleToken }
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();

        if (problemTokens.Length == 0)
        {
            return null;
        }

        foreach (var filePath in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ShouldSkip(filePath) || !SupportedExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            var fileName = Normalize(Path.GetFileNameWithoutExtension(filePath));
            if (!problemTokens.Any(token => fileName.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return new LocalSolution
            {
                FilePath = filePath,
                Language = ResolveLanguage(filePath),
                SourceCode = await File.ReadAllTextAsync(filePath, cancellationToken)
            };
        }

        return null;
    }

    public async Task<AcceptedSolutionLookup> FindAcceptedSolutionAsync(
        PotdProblem problem,
        CancellationToken cancellationToken = default)
    {
        var session = configuration["LeetCode:Session"];
        var csrfToken = configuration["LeetCode:CsrfToken"];

        if (string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(csrfToken))
        {
            return new AcceptedSolutionLookup
            {
                Message = "LeetCode session credentials are not configured, so accepted submissions cannot be checked."
            };
        }

        var submissionId = await FindLatestAcceptedSubmissionIdAsync(problem.Slug, cancellationToken);
        if (submissionId is null)
        {
            return new AcceptedSolutionLookup
            {
                Message = $"No accepted LeetCode submission was returned for slug '{problem.Slug}'."
            };
        }

        var submission = await GetSubmissionDetailsAsync(submissionId.Value, cancellationToken);
        if (submission is null)
        {
            return new AcceptedSolutionLookup
            {
                Message = $"Accepted submission {submissionId} was found, but LeetCode did not return submission details."
            };
        }

        if (string.IsNullOrWhiteSpace(submission.Code))
        {
            return new AcceptedSolutionLookup
            {
                Message = $"Accepted submission {submissionId} was found, but LeetCode returned empty code."
            };
        }

        if (!submission.StatusDisplay.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
        {
            return new AcceptedSolutionLookup
            {
                Message = $"Submission {submissionId} details returned status '{submission.StatusDisplay}' instead of Accepted."
            };
        }

        var root = ResolveSolutionsRoot(createIfMissing: true);
        var filePath = Path.Combine(root, $"{problem.Slug}{ResolveExtension(submission.Language)}");

        await File.WriteAllTextAsync(filePath, submission.Code, cancellationToken);

        return new AcceptedSolutionLookup
        {
            Message = $"Downloaded accepted LeetCode submission {submissionId} and saved it to {filePath}.",
            Solution = new LocalSolution
            {
                FilePath = filePath,
                Language = submission.Language,
                SourceCode = submission.Code,
                IsAcceptedLeetCodeSubmission = true
            }
        };
    }

    public Task<SubmissionResult> SubmitSolutionAsync(
        PotdProblem problem,
        LocalSolution solution,
        CancellationToken cancellationToken = default)
    {
        var session = configuration["LeetCode:Session"];
        var csrfToken = configuration["LeetCode:CsrfToken"];

        if (string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(csrfToken))
        {
            return Task.FromResult(new SubmissionResult
            {
                Status = ExecutionStatus.SubmissionUnavailable,
                Message = "LeetCode session credentials are not configured yet."
            });
        }

        return Task.FromResult(new SubmissionResult
        {
            Status = ExecutionStatus.SubmissionUnavailable,
            Message = "Authenticated LeetCode submission is intentionally behind ILeetCodeService and will be implemented next."
        });
    }

    public Task<SubmissionResult> GetSubmissionStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SubmissionResult
        {
            SubmissionId = submissionId,
            Status = ExecutionStatus.SubmissionUnavailable,
            Message = "Submission status polling is intentionally behind ILeetCodeService and will be implemented next."
        });
    }

    private async Task<int?> FindLatestAcceptedSubmissionIdAsync(
        string problemSlug,
        CancellationToken cancellationToken)
    {
        var graphQlSubmissionId = await FindLatestAcceptedSubmissionIdFromGraphQlAsync(
            problemSlug,
            cancellationToken);

        if (graphQlSubmissionId is not null)
        {
            return graphQlSubmissionId;
        }

        var url = $"https://leetcode.com/api/submissions/{problemSlug}/?offset=0&limit=20&lastkey=";
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("submissions_dump", out var submissions)
            || submissions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var submission in submissions.EnumerateArray())
        {
            var status = GetString(submission, "status_display");
            if (!status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryGetInt(submission, "id", out var id))
            {
                return id;
            }
        }

        return null;
    }

    private async Task<int?> FindLatestAcceptedSubmissionIdFromGraphQlAsync(
        string problemSlug,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "https://leetcode.com/graphql");
        request.Content = JsonContent.Create(new
        {
            operationName = "questionSubmissionList",
            query = """
                query questionSubmissionList($questionSlug: String!, $offset: Int!, $limit: Int!, $lastKey: String) {
                  questionSubmissionList(questionSlug: $questionSlug, offset: $offset, limit: $limit, lastKey: $lastKey) {
                    submissions {
                      id
                      statusDisplay
                    }
                  }
                }
                """,
            variables = new
            {
                questionSlug = problemSlug,
                offset = 0,
                limit = 20,
                lastKey = (string?)null
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("questionSubmissionList", out var submissionList)
            || !submissionList.TryGetProperty("submissions", out var submissions)
            || submissions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var submission in submissions.EnumerateArray())
        {
            var status = GetString(submission, "statusDisplay");
            if (!status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryGetInt(submission, "id", out var id))
            {
                return id;
            }
        }

        return null;
    }

    private async Task<AcceptedSubmission?> GetSubmissionDetailsAsync(
        int submissionId,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "https://leetcode.com/graphql");
        request.Content = JsonContent.Create(new
        {
            operationName = "submissionDetails",
            query = """
                query submissionDetails($submissionId: Int!) {
                  submissionDetails(submissionId: $submissionId) {
                    code
                    lang
                    statusDisplay
                  }
                }
                """,
            variables = new
            {
                submissionId
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("submissionDetails", out var details)
            || details.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return new AcceptedSubmission(
            GetString(details, "code"),
            GetString(details, "lang"),
            GetString(details, "statusDisplay"));
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var session = configuration["LeetCode:Session"];
        var csrfToken = configuration["LeetCode:CsrfToken"];

        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 LeetPOTD/1.0");
        request.Headers.Referrer = new Uri("https://leetcode.com/");

        if (!string.IsNullOrWhiteSpace(session) && !string.IsNullOrWhiteSpace(csrfToken))
        {
            request.Headers.Add("Cookie", $"LEETCODE_SESSION={session}; csrftoken={csrfToken}");
            request.Headers.Add("x-csrftoken", csrfToken);
        }

        return request;
    }

    private static bool ShouldSkip(string filePath)
    {
        var segments = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string ResolveLanguage(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".cpp" => "cpp",
            ".c" => "c",
            ".java" => "java",
            ".py" => "python3",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".go" => "golang",
            ".rs" => "rust",
            _ => "text"
        };
    }

    private string ResolveSolutionsRoot(bool createIfMissing = false)
    {
        var root = configuration["LeetCode:SolutionsPath"];
        if (string.IsNullOrWhiteSpace(root))
        {
            root = "solutions";
        }

        if (!Path.IsPathRooted(root))
        {
            root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, root));
        }

        if (createIfMissing)
        {
            Directory.CreateDirectory(root);
        }

        return root;
    }

    private static string ResolveExtension(string language)
    {
        return Normalize(language) switch
        {
            "csharp" => ".cs",
            "cpp" => ".cpp",
            "c" => ".c",
            "java" => ".java",
            "python" or "python3" => ".py",
            "javascript" => ".js",
            "typescript" => ".ts",
            "golang" or "go" => ".go",
            "rust" => ".rs",
            "mysql" or "mssql" or "oraclesql" or "postgresql" => ".sql",
            _ => ".txt"
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool TryGetInt(JsonElement element, string propertyName, out int value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var idElement))
        {
            return false;
        }

        if (idElement.ValueKind == JsonValueKind.Number)
        {
            return idElement.TryGetInt32(out value);
        }

        return idElement.ValueKind == JsonValueKind.String
            && int.TryParse(idElement.GetString(), out value);
    }

    private sealed record AcceptedSubmission(
        string Code,
        string Language,
        string StatusDisplay);
}
