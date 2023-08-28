<?php

function Routing($pageName, $specialParams = "") {
    echo '<script>';
    echo 'history.replaceState({}, null, "./?page='.$pageName.$specialParams;
    echo 'document.title = "SAMonitor - '.$pageName;
    echo '</script>';
}

?>