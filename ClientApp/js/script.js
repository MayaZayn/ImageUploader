let imageTitle = document.getElementById( "imageTitle" );
let imageFile = document.getElementById( "imageFile" );

function getExtension() {
    var parts = imageFile.value.split('.');
    return parts[parts.length - 1];
}
  
function validateImage() {
    var ext = getExtension();
    switch (ext.toLowerCase()) {
        case 'jpeg':
        case 'gif':
        case 'png':
        return;
    }

    alert("Invalid Extnesion");
    imageFile.value = "";
}

function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0,
            v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

var uniqueId = generateUUID();
var url = "/picture/" + uniqueId;

function upload(event) {
    event.preventDefault();

    const fileInput = imageFile;
    const file = fileInput.files[0];
    let fileContent;
    
    if (file) {
        const reader = new FileReader();
        reader.onload = function(event) {
            fileContent = event.target.result;

            const image = {
                title: imageTitle.value,
                content: fileContent,
                id: uniqueId
            };

            fetch('/upload', {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(image)
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
    
                // redirect
                window.location.href = url;
            })
            .catch(error => {
                console.error('There was a problem with the fetch operation:', error);
            });
        };
        reader.readAsDataURL(file);
    } else {
        console.error('No file selected.');
    }
}