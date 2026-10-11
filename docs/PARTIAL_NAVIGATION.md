# In-page updates

The app still renders Razor Pages on the server. `wwwroot/js/partial-navigation.js`
enhances explicitly marked links and forms with fetch; normal requests and
JavaScript-disabled browsers keep the existing full-page behavior. No package or
JavaScript framework was added.

## Converted interactions

- `/`: date and direction tabs update `_TripsList` (date/count header and trips).
  The hero remains in place; its city labels follow the selected direction.
- `/Office`: previous day, today, next day update the day's content and statistics.
- `/MyTickets`: previous/next pagination.
- `/Office/Settings/Stops`, `Drivers`, `Buses`, `Schedules`: edit/cancel links and saves.
- `/Office/Settings/Layouts`: create/save a seat layout.
- `/Office/Settings/Route`: direction selection and save.
- `/Office/Trip/{id}`: office booking, cancellation, and bus/driver assignment.

The existing handlers, validation and authorization still run. POSTs include all
form fields and the existing anti-forgery token. Successful same-page POST/redirect/GET
flows follow the redirect and swap the fresh fragment. Other redirects and
401/403 responses trigger ordinary navigation, never a login page inside a region.

## Deliberately normal navigation

Home, branding, office navigation/settings tiles, trip and ticket links, and
back links change pages. Language switching changes localization for the whole
document. Account forms, login/logout/Google, public booking leading to a ticket,
receipt upload, passenger cancellation, and payment confirmation/rejection keep
their existing navigation. Receipts open in their own tab. Phone, print, seat
selection and copy controls were already client-side.

## Opt in a new page

For a page whose body changes together, set `Layout = "_PageFragmentLayout"`
and add `data-partial="page-content"` to each same-page link or form to enhance.
Keep its real href/action/method. The shared layout wraps its existing markup;
it omits `_Layout` only for a matching `X-SBus-Region` request. No duplicated view
markup or new routes are needed. Pages using Razor sections need those sections
forwarded by the fragment layout before opting in; current converted pages have none.

For a smaller region, follow `Index`: render one partial normally and return
that same partial when `PartialRequests.Prepare(HttpContext, regionId)` is true.
Its wrapper has `id`, `data-partial-region`, `data-partial-url` (canonical GET URL),
and `data-partial-announcement`. Place `_PartialFeedback` outside the region,
passing its ID. The header is also added to `Vary`; partial responses use no-store.
Server-side partial rendering follows [ASP.NET Core's partial view support](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/partial?view=aspnetcore-10.0).

Optional `data-partial-controls`, `data-partial-param` and `data-partial-value`
provide immediate selected states and compose query parameters during rapid clicks.
`data-partial-text` with an HTML-encoded JSON `data-partial-values` map updates
labels outside the region. Other scripts initialize newly swapped content through
the delegated `sbus:updated` event in `site.js`.

GET requests use [fetch cancellation with AbortController](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch)
and a current-request check. POSTs are serialized, not canceled/replayed: if the
response is lost, the error button performs only a GET so users can inspect the
outcome before another write. Old data stays visible during loading/errors.
Same-page selections update history; Back/Forward refetch the selected state.
When scrolled, the region keeps its previous minimum height so shorter results
do not push the viewport upward. Updated trips omit the initial entrance animation.
Live announcements, keyboard tabs and focus restoration accompany swaps.

## Files and verification

New: `Infrastructure/PartialRequests.cs`, `Pages/Shared/_TripsList.cshtml`,
`_PageFragmentLayout.cshtml`, `_PartialFeedback.cshtml`,
`wwwroot/js/partial-navigation.js`, and `Web/PartialNavigationTests.cs`.
Modified: `Pages/Index.cshtml` and its PageModel; the converted views above;
`Pages/Shared/_Layout.cshtml`; `wwwroot/js/site.js`; shared loading/animation CSS rules
in `wwwroot/css/site.css`; English localization resources. Auth styling and Home
link changes already present in the workspace are earlier work.

Server integration tests use substituted application data and test authentication;
they do not access a database or send email. They cover all nine opted-in page
views, public selection normalization, authorization challenge, anti-forgery,
validation and unchanged same-page redirects. Browser verification uses that same
fixture on a temporary HTTPS host: fetch-only selection, unchanged document/hero,
scroll position, Back/Forward, shareable refresh, keyboard tabs, delayed stale
responses, inline failure/retry, office edit/save/validation, JS-disabled fallback,
and Arabic RTL/English LTR mobile layouts. Actual database writes and external
authentication are outside this presentation change and were not exercised.
