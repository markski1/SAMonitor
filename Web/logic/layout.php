<?php

function PageHeader($title) {
    if (isset($_SERVER['HTTP_HX_REQUEST'])) return;
    ?>

    <!DOCTYPE html>
    <html lang="en">
        <head>
            <title>SAMonitor - <?=$title?></title>
            
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta charset="utf-8">
            <link rel="stylesheet" type="text/css" href="style.css">
            
            <meta name="title" content="SAMonitor - <?=$title?>">
            <meta name="description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist.">

            <meta name="og:title" content="SAMonitor - <?=$title?>">
            <meta property="og:description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist.">

            <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
            <script src="https://unpkg.com/htmx.org@1.9.5/dist/htmx.min.js"></script>
        </head>
        <body>
            <header>
                <div class="headerContents">
                    <div>
                        <h1>SAMonitor</h1>
                    </div>
                    <div>
                        <a href="./" hx-get="./" hx-push-url="true" hx-target="#main">servers</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="about.php" hx-get="about.php" hx-push-url="true" hx-target="#main">about</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="masterlist.php" hx-get="masterlist.php" hx-push-url="true" hx-target="#main">masterlist mod</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="metrics.php" hx-get="metrics.php" hx-push-url="true" hx-target="#main">metrics</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="add.php" hx-get="add.php" hx-push-url="true" hx-target="#main">add server</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="donate.php" hx-get="donate.php" hx-push-url="true" hx-target="#main">donate</a>  <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="blacklist.php" hx-get="blacklist.php" hx-push-url="true" hx-target="#main">blacklist</a>
                    </div>
                </div>
            </header>
            <main id="main">
    <?php
}

function PageBottom() {
    if (isset($_SERVER['HTTP_HX_REQUEST'])) return;
    ?>

            </main>
        </body>
    </html>

    <script>
        function CopyAddress(id) {
            var copyText = document.getElementById(id);
            navigator.clipboard.writeText(copyText.innerText);
            alert("Address copied.");
        }
    </script>
    
<?php } ?>