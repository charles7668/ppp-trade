using System.Diagnostics;

namespace ppp_trade.Services;

public class RateLimitParser
{
    /// <summary>
    /// Calculates and waits for the required delay time based on rate limit information from response headers.
    /// This function is designed to adhere to the strictest rate limit rule.
    /// </summary>
    /// <param name="rateLimitRulesText">The raw rate limit rules, e.g., "aa:bb:cc,dd:ee:ff" (X-Rate-Limit-* header).</param>
    /// <param name="rateLimitStateText">The current rate limit state, e.g., "aa:bb:cc,dd:ee:ff" (X-Rate-Limit-*-State header).</param>
    public int GetWaitTimeForRateLimit(string? rateLimitRulesText, string? rateLimitStateText)
    {
        // 1. Check if input is null or empty
        if (string.IsNullOrWhiteSpace(rateLimitRulesText) || string.IsNullOrWhiteSpace(rateLimitStateText))
        {
            // Assume safety if rate limit headers are missing; no waiting required.
            return 0;
        }

        long maxWaitTimeMs = 0;

        // Parse rules and states
        var rules = rateLimitRulesText.Split(',');
        var states = rateLimitStateText.Split(',');

        // Ensure the number of rules matches the number of states
        if (rules.Length != states.Length)
        {
            Debug.WriteLine("[限速警告] 規則和狀態數量不匹配，跳過限速檢查。");
            return 0;
        }

        // Iterate through each rule and state pair
        for (var i = 0; i < rules.Length; i++)
        {
            if (!ParseRateLimitRule(rules[i], out var limit, out var windowSeconds, out var blackoutSeconds) ||
                !ParseRateLimitState(states[i], out var currentCalls, out var stateWindowSeconds,
                    out var currentBlackoutSeconds))
            {
                // Skip the rule if parsing failed
                continue;
            }

            // Ensure the Time Window (bb) for the rule and state match
            if (windowSeconds != stateWindowSeconds)
            {
                Debug.WriteLine($"[限速警告] 規則 {i + 1} 的時間窗口不匹配，跳過此規則。");
                continue;
            }

            // A. Handle Blackout Penalty (Highest priority)
            if (currentBlackoutSeconds > 0)
            {
                // If currently under penalty, must wait the remaining blackout time
                var waitMs = (long)currentBlackoutSeconds * 1000;
                maxWaitTimeMs = Math.Max(maxWaitTimeMs, waitMs);
                Debug.WriteLine($"[限速檢查] 規則 {i + 1} 處於懲罰期，需等待 {currentBlackoutSeconds} 秒。");
                continue; // Continue checking the next rule
            }

            // B. Handle Near-Exceeded Limit (Preemptive waiting)
            if (currentCalls >= limit)
            {
                // If the limit has been reached or exceeded (even if Blackout hasn't triggered).
                // Since we lack the exact reset time, we conservatively wait a small amount.

                // --- Note: In a production environment, you would use a "Reset Time" header if available. ---

                // If there's no Blackout and Calls >= Limit, we wait at least 1 second to avoid triggering a penalty.
                maxWaitTimeMs = Math.Max(maxWaitTimeMs, 1000);
                Debug.WriteLine($"[限速檢查] 規則 {i + 1} 已達上限 ({currentCalls}/{limit})，建議等待 1 秒以避免觸發懲罰。");
            }
        }

        return (int)maxWaitTimeMs;
    }

    /// <summary>
    /// Parses a single rate limit rule string. Format: Limit:Window:Blackout (aa:bb:cc)
    /// </summary>
    private bool ParseRateLimitRule(string ruleText, out int limit, out int windowSeconds, out int blackoutSeconds)
    {
        limit = windowSeconds = blackoutSeconds = 0;
        var parts = ruleText.Split(':');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out limit) &&
            int.TryParse(parts[1], out windowSeconds) &&
            int.TryParse(parts[2], out blackoutSeconds))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a single rate limit state string. Format: CurrentCalls:Window:BlackoutLeft (aa:bb:cc)
    /// </summary>
    private bool ParseRateLimitState(string stateText, out int currentCalls, out int stateWindowSeconds,
        out int currentBlackoutSeconds)
    {
        currentCalls = stateWindowSeconds = currentBlackoutSeconds = 0;
        var parts = stateText.Split(':');
        // POE Rate Limit State format is typically Calls:Window:BlackoutLeft
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out currentCalls) &&
            int.TryParse(parts[1], out stateWindowSeconds) &&
            int.TryParse(parts[2], out currentBlackoutSeconds))
        {
            return true;
        }

        return false;
    }
}