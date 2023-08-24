<?php ?>
<!DOCTYPE html>
<html>
    <head>
        <title>SAMonitor - SA-MP and open.mp server monitor</title>
        
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <link rel="stylesheet" type="text/css" href="styles.css">
        
        <meta name="title" content="SAMonitor">
        <meta name="description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist.">

        <meta name="og:title" content="SAMonitor" />
        <meta property="og:description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist." />
    </head>
    <body>
        <header>
            <div>
                <h1>SAMonitor</h1>
            </div>
            <div>
                <a href="" hx-get="view/servers.php" hx-target="#main">servers</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/about.php" hx-target="#main">about</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/api.php" hx-target="#main">api & masterlist</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/add.php" hx-target="#main">add server</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/blacklist.php" hx-target="#main">blacklist</a> <span class="separator">&nbsp;/&nbsp;</span>
                <!--<a href="" hx-get="view/sponsor.php" hx-target="#main">sponsor</a>  <span class="separator">&nbsp;/&nbsp;</span>-->
                <a href="https://github.com/markski1/SAMonitor">github</a>
            </div>
        </header>
        <main id="main" hx-get="view/servers.php" hx-trigger="load">
            
        </main>
    </body>
</html>
<script src="https://unpkg.com/htmx.org@1.9.4" integrity="sha384-zUfuhFKKZCbHTY6aRR46gxiqszMk5tcHjsVFxnUo8VMus4kHGVdIYVbOYYNlKmHV" crossorigin="anonymous"></script>
<script>
    function CopyAddress(id) {

        var copyText = document.getElementById(id);
               
        navigator.clipboard.writeText(copyText.innerText);
        
        alert("Address copied.");
    }
</script>