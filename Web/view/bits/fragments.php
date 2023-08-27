<?php

function DrawServer($server, $num, $details = false) {
    $server = array_map('htmlspecialchars', $server);

    if ($server['website'] != "Unknown") {
        $website = '<a href="'.$server['website'].'">'.$server['website'].'</a>';
    }
    else {
        $website = "No website specified.";
    }

    $lagcomp = $server['lagComp'] == 1 ? "Enabled" : "Disabled";
    $last_updated = strtotime($server['lastUpdated']);
?>
    <h2><?=$server['name']?></h2>
    <div>
        <fieldset>
            <legend class="gameMode"><?=$server['gameMode']?></legend>
            <p class="serverInfo">
                <b>Players</b>: <?=$server['playersOnline']?> / <?=$server['maxPlayers']?><br />
                <b>Language</b>: <?=$server['language']?>
            </p>

            <?php if ($details) { ?>
                <h3 style="margin: 1rem .2rem .4rem">Details</h3>
                <table class="serverDetailsTable">
                    <tr>
                        <td><b>Map</b></td><td><?=$server['mapName']?></td>
                    </tr>
                    <tr>
                        <td><b>Lag compensation</b></td><td><?=$lagcomp?></td>
                    </tr>
                    <tr>
                        <td><b>Version</b></td><td><?=$server['version']?></td>
                    </tr>
                    <tr>
                        <td><b>Website</b></td><td><?=$website?></td>
                    </tr>
                    <tr>
                        <td><b>SAMPCAC</b></td><td><?=$server['sampCac']?></td>
                    </tr>
                    <tr>
                        <td><b>Last updated</b></td><td><?=timeSince($last_updated)?> ago</td>
                    </tr>
                </table>
                <button hx-get="view/server.php?&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>" style="width: 100%; margin-top: 1rem;" hx-target="#main">All about this server</button>
            <?php } ?>
        </fieldset>
    </div>
    <div style="float: left">
        <p class="ipAddr" id="ipAddr<?=$num?>"><?=$server['ipAddr']?></p>
    </div>
    <div style="text-align: right; float: right;">
        <?php if (!$details) { ?>
            <button hx-get="view/bits/fragments.php?type=details&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>">Show details</button>
        <?php } ?>
        <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
    </div>
    <div style="clear: both"></div>
<?php
}

if (isset($_GET['type'])) {
    if ($_GET['type'] == 'details') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".urlencode($_GET['ip_addr'])), true);

        DrawServer($server, $_GET['number'], true);
    }

    if ($_GET['type'] == 'basic') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".urlencode($_GET['ip_addr'])), true);

        DrawServer($server, $_GET['number']);
    }

    //..
}

function timeSince($time) {
    $time = time() - $time; // to get the time since that moment
    $time = ($time<1)? 1 : $time;
    $tokens = array (
        31536000 => 'year',
        2592000 => 'month',
        604800 => 'week',
        86400 => 'day',
        3600 => 'hour',
        60 => 'minute',
        1 => 'second'
    );

    foreach ($tokens as $unit => $text) {
        if ($time < $unit) continue;
        $numberOfUnits = floor($time / $unit);
        return $numberOfUnits.' '.$text.(($numberOfUnits>1)?'s':'');
    }
}
?>