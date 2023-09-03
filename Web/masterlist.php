<?php
    include 'logic/layout.php';
    PageHeader("masterlist mod");
?>

<div>
    <h2>Masterlist mod</h3>
    <p>Integrate SAMonitor's listings right in your SA-MP client.</p>
    <div class="innerContent">
        <h3>Information</h3>
        <p>I am immensely happy to announce that SA-MP Masterlist Fix has been updated, and now uses SAMonitor as it's default masterlist.</p>

        <p>This mod will make your SA-MP client use SAMonitor for the Internet tab.</p>
        
        <p>This comes with various benefits, the largest being: You will be able to see all servers in SA-MP, even after the official masterlist shuts down.</p>

        <figure style="margin: .5rem">
            <img style="width: 100%; max-width: 640px" src="assets/client-showcase.png">
            <figcaption style="font-size: 0.9rem">Look at all those servers!</figcaption>
        </figure>
    </div>

    <div class="innerContent">
        <h3>Installation</h3>
        <h4>1. Get SA-MP</h4>
        <p>
            Of course, the first step is to install SA-MP. The mod works with both 0.3.7 and 0.3DL. <br />
            While it should work with any version of 0.3.7, I strongly suggest you use the latest R5. <a href="https://sa-mp.com/download.php">Download here</a>.
        </p>
        <h4>2. Get SA-MP Masterlist Fix</h4>
        <p>
            Once SA-MP is installed, proceed to download the latest version of SA-MP Masterlist Mod. <a href="https://github.com/spmn/sa-mp_masterlist_fix/releases/latest">Download here</a>.
        </p>
        <h4>3. Install the mod</h4>
        <p>
            The page above explains the installation, but if needed, here's it is:
        </p>
        <ol type="1" style="line-height: 1.7rem">
            <li>Go to the SA-MP Masterlist Fix download site, linked above.</li>
            <li>Scroll down, and download the "version.dll" file.</li>
            <li>Go to your Downloads folder, and copy the "version.dll" file.</li>
            <li>Go to the folder where your GTA:SA is installed, and paste "version.dll".</li>
            <li>Open SA-MP, and check the Internet tab. It should be working.</li>
        </ol>
        <p>And if you really want to make sure, here's a quick video:</p>
        <video style="width: 100%; max-width: 768px" controls>
            <source src="assets/tutorial.mp4" type="video/mp4">
            Your browser does not support the video tag.
        </video> 
    </div>

    <div class="innerContent">
        <h3>Attribution</h3>
        <p>I am extremely thankful to spmn, the author of 'SA-MP Masterlist Fix', for taking the time to rewrite his mod.</p>
        <p>We both returned to our projects as soon as we heard about the upcoming shutdown of SA-MP's operations, and I am very proud of our work.</p>
    </div>
</div>

<script>
    document.title = "SAMonitor - masterlist mod";
</script>

<?php PageBottom() ?>