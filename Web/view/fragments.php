<?php

function DrawServer($server, $num, $details = false) {
    $server = array_map('htmlspecialchars', $server);
?>

    <h2><?=$server['name']?></h2>
    <div>
        <fieldset>
            <legend style="font-size: 1.5rem; font-weight: 400"><?=$server['gameMode']?></legend>
            <span style="font-weight: 400; font-size: 1.2rem" id="ipAddr<?=$num?>"><?=$server['ipAddr']?></span><br />
            
            <p style="margin: .33rem 0">
                <b><?=$server['playersOnline']?>/<?=$server['maxPlayers']?></b> players / Language: <?=$server['language']?>
            </p>
        </fieldset>
    </div>
    <div style="text-align: right">
        <button hx-get="view/fragments.php?type=details&ip_addr=<?=$server['ipAddr']?>&number=<?=$num?>">More details</button> <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
    </div>
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
                <legend style="font-size: 1.5rem; font-weight: 400"><?=$server['gameMode']?></legend>
                <span style="font-weight: 400; font-size: 1.2rem" id="ipAddr<?=$num?>"><?=$server['ipAddr']?></span><br />

                <p style="margin: .33rem 0">
                    <b><?=$server['playersOnline']?>/<?=$server['maxPlayers']?></b> players / Language: <?=$server['language']?>
                </p>
                <fieldset>
                    <legend>Details</legend>
                    <table style="width: 100%; font-weight: 400; text-align: left">
                        <tr>
                            <th><b>Map</b></th><th><?=$server['mapName']?></th>
                        </tr>
                        <tr>
                            <th><b>Lag compensation</b></th><th><?=$lagcomp?></th>
                        </tr>
                        <tr>
                            <th><b>Version</b></th><th><?=$server['version']?></th>
                        </tr>
                        <tr>
                            <th><b>Website</b></th><th><?=$website?></th>
                        </tr>
                        <tr>
                            <th><b>SAMPCAC</b></th><th><?=$server['sampCac']?></th>
                        </tr>
                        <tr>
                            <th><b>Last updated</b></th><th><?=timeSince($last_updated)?> ago</th>
                        </tr>
                    </table>
                </fieldset>
                <fieldset>
                    <legend>Player list</legend>
                    <iframe style="width: 93%; height: 5rem; border: 0" src="view/playerlist.php?ip_addr=<?=$server['ipAddr']?>&players=<?=$server['playersOnline']?>">
                    </iframe>
                </fieldset>
            </fieldset>
        </div>
        <div style="text-align: right">
        <button class="connectButton" onclick="CopyAddress('ipAddr<?=$num?>')">Copy IP</button>
        </div>
    <?php
    }

if (isset($_GET['type'])) {
    if ($_GET['type'] == 'details') {
        $server = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=".$_GET['ip_addr']), true);

        DrawServerDetailed($server, $_GET['number']);
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

