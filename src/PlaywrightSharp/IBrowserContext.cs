using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserContexts provide a way to operate multiple independent browser sessions.
    /// If a <see cref="IPage"/> opens another page, e.g.with a window.open call, the popup will belong to the parent page's browser context.
    /// PlaywrightSharp allows creation of "incognito" browser contexts with <seealso cref="IBrowser.NewContextAsync"/> method. "Incognito" browser contexts don't write any browsing data to disk.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Create a new incognito browser context
    /// const context = await browser.NewContextAsync();
    /// // Create a new page inside context.
    /// const page = await context.NewPageAsync("https://example.com");
    /// // Dispose context once it's no longer needed.
    /// await context.CloseAsync();
    /// ]]>
    /// </code>
    /// </example>
    public interface IBrowserContext : IAsyncDisposable
    {
        /// <summary>
        /// Raised when Browser context gets closed.
        /// </summary>
        event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Raised when a new page is created in the Browser context.
        /// </summary>
        event EventHandler<PageEventArgs> PageCreated;

        /// <summary>
        /// This setting will change the default maximum time for all the methods accepting timeout option.
        /// </summary>
        public int DefaultTimeout { get; set; }

        /// <summary>
        /// An array of all pages inside the browser context.
        /// </summary>
        IPage[] Pages { get; }

        /// <summary>
        /// Options used to create the context.
        /// </summary>
        BrowserContextOptions Options { get; }

        /// <summary>
        /// Browser owner.
        /// </summary>
        Browser Browser { get; }

        /// <summary>
        /// Creates a new page in the browser context and optionally navigates it to the specified URL.
        /// </summary>
        /// <param name="url">URL to navigate after the page is created.</param>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created, yielding the new <see cref="IPage"/>.</returns>.
        Task<IPage> NewPageAsync(string url = null);

        /// <summary>
        /// Closes the browser context. All the targets that belong to the browser context will be closed.
        /// </summary>
        /// <remarks>NOTE only incognito browser contexts can be closed.</remarks>
        /// <returns>A <see cref="Task"/> that completes when the browser context is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Returns the context's cookies.
        /// </summary>
        /// <param name="urls">Url's to return cookies for.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are sent by the browser, yielding a <see cref="t:NetworkCookie[]"/>.</returns>
        /// <remarks>
        /// If no URLs are specified, this method returns cookies for the current page URL.
        /// If URLs are specified, only cookies for those URLs are returned.
        /// </remarks>
        Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls);

        /// <summary>
        /// Clears all of the current cookies and then sets the cookies for the context.
        /// </summary>
        /// <param name="cookies">Cookies to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are set.</returns>
        Task SetCookiesAsync(params SetNetworkCookieParam[] cookies);

        /// <summary>
        /// Clears the context's cookies.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the cookies are cleared.</returns>
        Task ClearCookiesAsync();

        /// <summary>
        /// Grants permissions to an URL.
        /// </summary>
        /// <param name="origin">The origin to grant permissions to, e.g. "https://example.com".</param>
        /// <param name="permissions">An array of permissions to grant.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetPermissionsAsync(string origin, params ContextPermission[] permissions);

        /// <summary>
        /// Sets the page's geolocation.
        /// </summary>
        /// <param name="geolocation">Geolocation.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetGeolocationAsync(GeolocationOption geolocation);

        /// <summary>
        /// Clears all permission overrides for the browser context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ClearPermissionsAsync();

        /// <summary>
        /// Gets all the existing pages in the context.
        /// </summary>
        /// <returns>A list of pages.</returns>
        IEnumerable<IPage> GetExistingPages();

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function.
        /// </summary>
        /// <param name="e">Event to wait for.</param>
        /// <param name="options">Extra options.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEvent<T>(ContextEvent e, WaitForEventOptions<T> options = null);
    }
}