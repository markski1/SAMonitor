<?php ?>
<div>
    <h2>About</h3>
    <p>SAMonitor is a SA-MP and open.mp server monitor. It tracks activity of them and is a good replacement of standard listings.</p>
    <div class="innerContent">
        <h3>Project</h3>
        <p>SAMonitor intends to be a "spiritual successor" of SACNR Monitor. It is a completely free and open source project. All code and API documentation is in <a href="https://github.com/markski1/SAMonitor">SAMonitor's GitHub monorepo</a>.</p>

        <p>It actively monitors for repeated servers, faked statistics and other illegitimate things.<br />As such, it provides higher quality listings than most other places, as it blocks repeated and fake entries.</p>

        <p>If you wish to contact me, please check <a href="https://markski.ar">my personal website</a> for contact methods.</p>
    </div>

    <div class="innerContent">
        <h3>Functionality</h3>
        <p>SAMonitor's objectives are very clear:</p>
        <ul>
            <li>Monitor every SA-MP and open.mp server.</li>
            <li>Maintain a free and open web interface (you are here) for people to access the data.</li>
            <li>Maintain a free and open API for developers to use the data.</li>
            <li>Don't have repeated servers or any other type of clutter.</li>
            <li>Blacklist illegitimate servers, be them stolen scripts, malicious or faking data.</li>
        </ul>
        <p>SAMonitor's listings only include servers which replied to queries in the last 24 hours.</p>
        <p>Servers which fail to reply to queries in 24 hours are removed for public listings, but SAMonitor will keep it in record for 14 more days, constantly querying for it to come back. If a server doesn't return after 14 days, it is permanently removed.</p>
        <p>Every server is queried every 20 minutes (3 times per hour). It will update it's listing data when this happens, and store the amount of players at that time into the database.</p>
        <p>SAMonitor keeps a constant, real-time count of servers and players online across the globe. These are stored every 30 minutes and visible in the "metrics" page.</p>
    </div>
    <div class="innerContent">
        <h3>Third party attributions</h3>
        <p>HTMX by Carson Gross is used for the website's presentation. <a href="https://htmx.org">htmx.org</a></p>
        <p>Chart.Js, by multiple people, is used for drawing the website's graphs. <a href="https://www.chartjs.org/">chartjs.org</a></p>
        <p>SAMPQuery by JustMavi is used for querying on the API. <a href="https://github.com/justmavi/sampquery/">github.com/justmavi/sampquery</a></p>
    </div>
</div>

<script>
    history.replaceState({}, null, "./?page=about");
    document.title = "SAMonitor - about";  
</script>