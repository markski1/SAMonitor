<?php
    // routing
    $page = htmlspecialchars($_GET['page'] ?? "servers");

    // title
    if ($page == "servers") {
        $title = "SA-MP and open.mp server monitor";
    }
    else {
        $title = $page;
    }

    $loadPage = "view/{$page}.php";

    // if looking at single-server page..
    if ($page == "server") {
        if (!isset($_GET['ip_addr'])) {
            $ip_addr = "invalid";
        }
        else {
            $ip_addr = trim($_GET['ip_addr']);
        }
        $loadPage = $loadPage."?ip_addr={$ip_addr}";
    }
?>
<!DOCTYPE html>
<html lang="en">
    <head>
        <title>SAMonitor - <?=$title?></title>
        
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta charset="utf-8">
        <link rel="stylesheet" type="text/css" href="styles.css">
        
        <meta name="title" content="SAMonitor - <?=$title?>">
        <meta name="description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist.">

        <meta name="og:title" content="SAMonitor - <?=$title?>" />
        <meta property="og:description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist." />
    </head>
    <body>
        <header>
            <div>
                <h1>SAMonitor</h1>
            </div>
            <div>
                <a href="?page=servers" hx-get="view/servers.php" hx-target="#main">servers</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="?page=about" hx-get="view/about.php" hx-target="#main">about</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="?page=metrics" hx-get="view/metrics.php" hx-target="#main">metrics</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="?page=add" hx-get="view/add.php" hx-target="#main">add server</a> <span class="separator">&nbsp;/&nbsp;</span>
                <!--<a href="?page=sponsor" hx-get="view/sponsor.php" hx-target="#main">sponsor</a>  <span class="separator">&nbsp;/&nbsp;</span>-->
                <a href="?page=blacklist" hx-get="view/blacklist.php" hx-target="#main">blacklist</a>
            </div>
        </header>
        <main id="main" hx-get="<?=$loadPage?>" hx-trigger="load"></main>
    </body>
</html>
<script src="https://unpkg.com/htmx.org@1.9.4/dist/htmx.min.js" crossorigin="anonymous"></script>

<script>
    function CopyAddress(id) {
        var copyText = document.getElementById(id);
        navigator.clipboard.writeText(copyText.innerText);
        alert("Address copied.");
    }
</script>

<script>
    window.onpopstate = function(e){
        if (e.state) {
            document.getElementById("main").innerHTML = e.state;
        }
    };
</script>