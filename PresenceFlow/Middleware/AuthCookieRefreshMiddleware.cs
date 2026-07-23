using PresenceFlow.Services;

namespace PresenceFlow.Middleware
{
    public class AuthCookieRefreshMiddleware
    {
        // RequestDelegate repräsentiert die nächste Komponente in der HTTP-Pipeline.
        // Jeder HTTP-Request wird nacheinander durch alle registrierten Middleware-Komponenten geleitet.
        private readonly RequestDelegate _next;

        public AuthCookieRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Prüft bei jedem HTTP-Request automatisch, ob ein gültiges Auth-Cookie existiert,
        /// und verlängert dessen Ablaufdatum (z. B. auf 30 Tage), bevor der Request weitergereicht wird.
        /// </summary>
        /// <remarks>
        /// Wird automatisch von der HTTP-Pipeline aufgerufen, sobald die Middleware mit
        /// app.UseMiddleware<AuthCookieRefreshMiddleware>() registriert wurde.
        /// Razor-Komponenten wie Home.razor müssen nichts explizit aufrufen.
        /// 
        /// Wichtig:
        /// - Läuft nur bei echten HTTP-Requests, nicht bei Hintergrund-Threads.
        /// - Verlängert das Cookie nur, wenn es gültig ist.
        /// - Danach wird der Request an die nächste Middleware oder Komponente weitergereicht (_next).
        /// </remarks>
        public async Task InvokeAsync(
            HttpContext context,
            IAuthCookieService authCookieService)
        {
            // Prüfen, ob das Auth-Cookie existiert
            if (context.Request.Cookies.ContainsKey("PresenceFlowAuth"))
            {
                // Cookie nur verlängern, wenn es gültig ist
                await authCookieService.RefreshAsync();
            }

            // Request an die nächste Middleware oder Komponente weitergeben
            await _next(context);
        }
    }
}
