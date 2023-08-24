<?php
    include 'fragments.php';

    // no sanitiztion is required on these GET parameters as these just call the public API.

    $filters = "?";

    if (isset($_GET['show_empty'])) {
        $filters .= "show_empty=1";
    }
    else {
        $filters .= "no_empty";
    }

    $order = $_GET['order'] ?? "player";

    $filters .= "&order=".$order;

    $page = $_GET['page'] ?? 0;

    $filters .= "&paging_size=20";

    if (isset($_GET['name']) && strlen($_GET['name']) > 0) {
        $filters .= "&name=".$_GET['name'];
    }

    if (isset($_GET['gamemode']) && strlen($_GET['gamemode']) > 0) {
        $filters .= "&gamemode=".$_GET['gamemode'];
    }

    $servers = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetFilteredServers" . $filters . "&page=".$page), true);

    echo "";

    $num = 0;
    foreach ($servers as $server) {
        echo '<div hx-target="this" class="server">';
        DrawServer($server, $num);
        echo '</div>';
        $num++;
    }

    echo '
        <div hx-target="this">
            <center><button hx-trigger="click" hx-get="./view/list.php'.$filters.'&page='.($page + 1).'" hx-swap="outerHTML">Load more</button></center>
        </div>
    ';
?>