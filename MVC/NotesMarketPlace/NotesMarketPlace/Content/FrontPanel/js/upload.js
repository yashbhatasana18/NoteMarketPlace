// File Upload
function DisplayPictureUpload() {
    function Init() {
        console.log("Display Picture Upload Initialised");

        var fileSelect = document.getElementById("file-upload"),
            fileDrag = document.getElementById("file-drag");

        fileSelect.addEventListener("change", fileSelectHandler, false);

        // Is XHR2 available?
        var xhr = new XMLHttpRequest();
        if (xhr.upload) {
            // File Drop
            fileDrag.addEventListener("dragover", fileDragHover, false);
            fileDrag.addEventListener("dragleave", fileDragHover, false);
            fileDrag.addEventListener("drop", fileSelectHandler, false);
        }
    }

    function fileDragHover(e) {
        var fileDrag = document.getElementById("file-drag");

        e.stopPropagation();
        e.preventDefault();

        fileDrag.className =
            e.type === "dragover" ? "hover" : "modal-body file-upload";
    }

    function fileSelectHandler(e) {
        // Fetch FileList object
        var files = e.target.files || e.dataTransfer.files;

        // Cancel event and hover styling
        fileDragHover(e);

        // Process all File objects
        for (var i = 0, f; (f = files[i]); i++) {
            parseFile(f);
            uploadFile(f);
        }
    }

    // Output
    function output(msg) {
        // Response
        var m = document.getElementById("messages");
        m.innerHTML = msg;
    }

    function parseFile(file) {
        console.log(file.name);
        output("<strong>" + encodeURI(file.name) + "</strong>");

        // var fileType = file.type;
        // console.log(fileType);
        var imageName = file.name;

        var isGood = /\.(?=jpg|png|jpeg)/gi.test(imageName);
        if (isGood) {
            document.getElementById("start").classList.add("hidden");
            document.getElementById("response").classList.remove("hidden");
            document.getElementById("notimage").classList.add("hidden");
            // Thumbnail Preview
            document.getElementById("file-image").classList.remove("hidden");
            document.getElementById("file-image").src = URL.createObjectURL(file);
        } else {
            document.getElementById("file-image").classList.add("hidden");
            document.getElementById("notimage").classList.remove("hidden");
            document.getElementById("start").classList.remove("hidden");
            document.getElementById("response").classList.add("hidden");
            document.getElementById("file-upload-form").reset();
        }
    }

    function uploadFile(file) {
        var xhr = new XMLHttpRequest(),
            fileInput = document.getElementById("class-roster-file"),
            fileSizeLimit = 1024; // In MB
        if (xhr.upload) {
            // Check if file is less than x MB
            if (file.size <= fileSizeLimit * 1024 * 1024) {

                // File received / failed
                xhr.onreadystatechange = function (e) {
                    if (xhr.readyState == 4) {
                        // Everything is good!
                        // progress.className = (xhr.status == 200 ? "success" : "failure");
                        // document.location.reload(true);
                    }
                };

                // Start upload
                xhr.open(
                    "POST",
                    document.getElementById("file-upload-form").action,
                    true
                );
                xhr.setRequestHeader("X-File-Name", file.name);
                xhr.setRequestHeader("X-File-Size", file.size);
                xhr.setRequestHeader("Content-Type", "multipart/form-data");
                xhr.send(file);
            } else {
                output("Please upload a smaller file (< " + fileSizeLimit + " MB).");
            }
        }
    }

    // Check for the various File API support.
    if (window.File && window.FileList && window.FileReader) {
        Init();
    } else {
        document.getElementById("file-drag").style.display = "none";
    }
}

function UploadNotes() {
    function Init() {
        console.log("Upload Notes Initialised");

        var fileSelect2 = document.getElementById("pdf-upload"),
            fileDrag2 = document.getElementById("pdf-drag");

        fileSelect2.addEventListener("change", fileSelectHandler2, false);

        // Is XHR2 available?
        var xhr2 = new XMLHttpRequest();
        if (xhr2.upload) {
            // File Drop
            fileDrag2.addEventListener("dragover", fileDragHover2, false);
            fileDrag2.addEventListener("dragleave", fileDragHover2, false);
            fileDrag2.addEventListener("drop", fileSelectHandler2, false);
        }
    }

    function fileDragHover2(e) {
        var fileDrag2 = document.getElementById("pdf-drag");

        e.stopPropagation();
        e.preventDefault();

        fileDrag2.className =
            e.type === "dragover" ? "hover" : "modal-body file-upload";
    }

    function fileSelectHandler2(e) {
        // Fetch FileList object
        var files2 = e.target.files || e.dataTransfer.files;

        // Cancel event and hover styling
        fileDragHover2(e);

        // Process all File objects
        for (var i = 0, f; (f = files2[i]); i++) {
            parseFile2(f);
            uploadFile2(f);
        }
    }

    // Output
    function output2(msg) {
        // Response
        var m2 = document.getElementById("messages2");
        m2.innerHTML = msg;
    }

    function parseFile2(file) {
        console.log(file.name);
        output2("<strong>" + encodeURI(file.name) + "</strong>");

        // var fileType = file.type;
        // console.log(fileType);
        var imageName2 = file.name;

        var isGood2 = /\.(?=pdf)/gi.test(imageName2);
        if (isGood2) {
            document.getElementById("start2").classList.add("hidden");
            document.getElementById("response2").classList.remove("hidden");
            document.getElementById("notimage2").classList.add("hidden");
            // Thumbnail Preview
            document.getElementById("file-pdf").classList.remove("hidden");
            document.getElementById("file-pdf").src = URL.createObjectURL(file);
        } else {
            document.getElementById("file-pdf").classList.add("hidden");
            document.getElementById("notimage2").classList.remove("hidden");
            document.getElementById("start2").classList.remove("hidden");
            document.getElementById("response2").classList.add("hidden");
            document.getElementById("pdf-upload-form").reset();
        }
    }

    function uploadFile2(file) {
        var xhr2 = new XMLHttpRequest(),
            fileInput = document.getElementById("class-roster-file"),
            fileSizeLimit = 1024; // In MB
        if (xhr2.upload) {
            // Check if file is less than x MB
            if (file.size <= fileSizeLimit * 1024 * 1024) {

                // File received / failed
                xhr2.onreadystatechange = function (e) {
                    if (xhr2.readyState == 4) {
                        // Everything is good!
                        // progress.className = (xhr.status == 200 ? "success" : "failure");
                        // document.location.reload(true);
                    }
                };

                // Start upload
                xhr2.open(
                    "POST",
                    document.getElementById("pdf-upload-form").action,
                    true
                );
                xhr2.setRequestHeader("X-File-Name", file.name);
                xhr2.setRequestHeader("X-File-Size", file.size);
                xhr2.setRequestHeader("Content-Type", "multipart/form-data");
                xhr2.send(file);
            } else {
                output("Please upload a smaller file (< " + fileSizeLimit + " MB).");
            }
        }
    }

    // Check for the various File API support.
    if (window.File && window.FileList && window.FileReader) {
        Init();
    } else {
        document.getElementById("pdf-drag").style.display = "none";
    }
}

function PreviewUploadNotes() {
    function Init() {
        console.log("Preview Upload Notes Initialised");

        var fileSelect3 = document.getElementById("preview-upload"),
            fileDrag3 = document.getElementById("preview-drag");

        fileSelect3.addEventListener("change", fileSelectHandler3, false);

        // Is XHR3 available?
        var xhr3 = new XMLHttpRequest();
        if (xhr3.upload) {
            // File Drop
            fileDrag3.addEventListener("dragover", fileDragHover3, false);
            fileDrag3.addEventListener("dragleave", fileDragHover3, false);
            fileDrag3.addEventListener("drop", fileSelectHandler3, false);
        }
    }

    function fileDragHover3(e) {
        var fileDrag3 = document.getElementById("preview-drag");

        e.stopPropagation();
        e.preventDefault();

        fileDrag3.className =
            e.type === "dragover" ? "hover" : "modal-body file-upload";
    }

    function fileSelectHandler3(e) {
        // Fetch FileList object
        var files3 = e.target.files || e.dataTransfer.files;

        // Cancel event and hover styling
        fileDragHover3(e);

        // Process all File objects
        for (var i = 0, f; (f = files3[i]); i++) {
            parseFile3(f);
            uploadFile3(f);
        }
    }

    // Output
    function output3(msg) {
        // Response
        var m3 = document.getElementById("messages3");
        m3.innerHTML = msg;
    }

    function parseFile3(file) {
        console.log(file.name);
        output3("<strong>" + encodeURI(file.name) + "</strong>");

        // var fileType = file.type;
        // console.log(fileType);
        var imageName3 = file.name;

        var isGood3 = /\.(?=pdf)/gi.test(imageName3);
        if (isGood3) {
            document.getElementById("start3").classList.add("hidden");
            document.getElementById("response3").classList.remove("hidden");
            document.getElementById("notimage3").classList.add("hidden");
            // Thumbnail Preview
            document.getElementById("file-preview").classList.remove("hidden");
            document.getElementById("file-preview").src = URL.createObjectURL(file);
        } else {
            document.getElementById("file-preview").classList.add("hidden");
            document.getElementById("notimage3").classList.remove("hidden");
            document.getElementById("start3").classList.remove("hidden");
            document.getElementById("response3").classList.add("hidden");
            document.getElementById("preview-upload-form").reset();
        }
    }

    function uploadFile3(file) {
        var xhr3 = new XMLHttpRequest(),
            fileInput = document.getElementById("class-roster-file"),
            fileSizeLimit = 1024; // In MB
        if (xhr3.upload) {
            // Check if file is less than x MB
            if (file.size <= fileSizeLimit * 1024 * 1024) {

                // File received / failed
                xhr3.onreadystatechange = function (e) {
                    if (xhr3.readyState == 4) {
                        // Everything is good!
                        // progress.className = (xhr.status == 200 ? "success" : "failure");
                        // document.location.reload(true);
                    }
                };

                // Start upload
                xhr3.open(
                    "POST",
                    document.getElementById("preview-upload-form").action,
                    true
                );
                xhr3.setRequestHeader("X-File-Name", file.name);
                xhr3.setRequestHeader("X-File-Size", file.size);
                xhr3.setRequestHeader("Content-Type", "multipart/form-data");
                xhr3.send(file);
            } else {
                output("Please upload a smaller file (< " + fileSizeLimit + " MB).");
            }
        }
    }

    // Check for the various File API support.
    if (window.File && window.FileList && window.FileReader) {
        Init();
    } else {
        document.getElementById("preview-drag").style.display = "none";
    }
}

DisplayPictureUpload();
UploadNotes();
PreviewUploadNotes();