<?php

function PageHeader($title, $description = null) {
    if (isset($_SERVER['HTTP_HX_REQUEST'])) return;

    $showDescription = $description ?? "A server monitor for SA-MP servers and open.mp servers. San Andreas Multiplayer public API and Masterlist.";

    ?>

    <!DOCTYPE html>
    <html lang="en">
        <head>
            <title>SAMonitor - <?=$title?></title>
            
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta charset="utf-8">
            <link rel="stylesheet" type="text/css" href="style.css">
            
            <meta name="title" content="SAMonitor - <?=$title?>">
            <meta name="description" content="<?=$showDescription?>">

            <meta name="og:title" content="SAMonitor - <?=$title?>">
            <meta property="og:description" content="<?=$showDescription?>">

            <script src="./assets/chart.js"></script>
            <script defer src="./assets/htmx.min.js"></script>
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
                        <a href="masterlist.php" hx-get="masterlist.php" hx-push-url="true" hx-target="#main"><span style="color: cyan">[!]</span> masterlist</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="metrics.php" hx-get="metrics.php" hx-push-url="true" hx-target="#main">metrics</a> <span class="separator">&nbsp;/&nbsp;</span>
                        <a href="add.php" hx-get="add.php" hx-push-url="true" hx-target="#main">add server</a> <span class="separator">&nbsp;/&nbsp;</span>
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
        var lastUsedCopyButton;

        function CopyAddress(ipID, buttonID) {
            var copyText = document.getElementById(ipID);
            navigator.clipboard.writeText(copyText.innerText);
            lastUsedCopyButton = document.getElementById(buttonID)
            lastUsedCopyButton.innerHTML = "IP Copied!";
            setTimeout(function () {
                lastUsedCopyButton.innerHTML = "Copy IP";
            }, 3000);
        }
    </script>
    
<?php } ?>