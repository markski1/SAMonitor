<?php

function DrawServer($server, $num) {
    $server = array_map('htmlspecialchars', $server);
?>

    <h2><?=$server['name']?></h2>
    <div>
        <fieldset>
            <legend class="gameMode"><?=$server['gameMode']?></legend>
            <p class="serverInfo">
                <b>Players</b>: <?=$server['playersOnline']?> / <?=$server['maxPlayers']?><br />
                <b>Language</b>: <?=$server['language']?>
            </p>
        </fieldset>
    </div>
    <div style="float: left">
        <p class="ipAddr" id="ipAddr<?=$num?>"><?=$server['ipAddr']?></p>
    </div>
    <div style="text-align: right; float: right;">
        <button hx-get="view/bits/fragments.php?type=details&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>">More details</button> <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
    </div>
    <div style="clear: both"></div>
<?php
}

function DrawServerDetailed($server, $num) {
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
                <fieldset>
                    <legend>Details</legend>
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
                </fieldset>
                <fieldset>
                    <legend>Player list</legend>
                    <iframe style="width: 93%; height: 6rem; border: 0" src="view/bits/playerlist.php?ip_addr=<?=$server['ipAddr']?>&players=<?=$server['playersOnline']?>"></iframe>
                </fieldset>
            </fieldset>
        </div>
        <div style="float: left">
            <p class="ipAddr" id="ipAddr<?=$num?>"><?=$server['ipAddr']?></p>
        </div>
        <div style="text-align: right; float: right;">
            <button hx-get="view/bits/fragments.php?type=basic&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>">Less details</button> <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
        </div>
        <div style="clear: both"></div>
    <?php
    }

if (isset($_GET['type'])) {
    if ($_GET['type'] == 'details') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".$_GET['ip_addr']), true);

        DrawServerDetailed($server, $_GET['number']);
    }

    if ($_GET['type'] == 'basic') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".$_GET['ip_addr']), true);

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