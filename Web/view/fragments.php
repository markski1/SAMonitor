<?php

function DrawServer($server, $num) {
    if ($server['website'] != "Unknown") {
        $website = 'Website: <a href="'.$server['website'].'">'.$server['website'].'</a>';
    }
    else {
        $website = "No website specified.";
    }
?>
    <h2><?=$server['name']?></h2>
    <div>
        <ul>
            <li><b>Players</b>: <?=$server['playersOnline']?>/<?=$server['maxPlayers']?></li>
            <li><b>Gamemode</b>: <?=$server['gameMode']?></li>
            <li><b>Language</b>: <?=$server['language']?></li>
        </ul>
    </div>
    <span>IP: <span id="ipAddr<?=$num?>"><?=$server['ipAddr']?></span>
    <div>
        <a href="" hx-get="view/fragments.php?type=details&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>"><button>More details</button></a> <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
    </div>
    <small>
        <div style="text-align: right">
            <?=$website?>
        </div>
    </small>
<?php
}

function DrawServerDetailed($server, $num) {
    if ($server['website'] != "Unknown") {
        $website = 'Website: <a href="'.$server['website'].'">'.$server['website'].'</a>';
    }
    else {
        $website = "No website specified.";
    }

    $lagcomp = $server['lagComp'] == 1 ? "Enabled" : "Disabled";

    ?>
        <h2><?=$server['name']?></h2>
        <ul>
            <li><b>Players</b>: <?=$server['playersOnline']?>/<?=$server['maxPlayers']?></li>
            <li><b>Gamemode</b>: <?=$server['gameMode']?></li>
            <li><b>Language</b>: <?=$server['language']?></li>
            <li><b>Map</b>: <?=$server['mapName']?></li>
            <li><b>World time</b>: <?=$server['worldTime']?></li>
            <li><b>Lag</b> compensation: <?=$lagcomp?></li> 
            <li><b>SAMPCAC</b>: <?=$server['sampCac']?></li> 
        </ul>
        <span>IP: <span id="ipAddr<?=$num?>"><?=$server['ipAddr']?></span>
        <div>
            <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
        </div>
        <small>
            <div style="text-align: right">
                <?=$website?><br />
                <b>Last updated:</b> <?=$server['lastUpdated']?>
            </div>
        </small>
    <?php
    }

if (isset($_GET['type'])) {
    if ($_GET['type'] == 'details') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".$_GET['ip_addr']), true);

        DrawServerDetailed($server, $_GET['number']);
    }

    //..
}

?>

