
function downloadFile(uri, fileName) {
    var link = document.createElement("a");
    link.setAttribute('download', fileName);
    link.href = uri;
    document.body.appendChild(link);
    link.click();
    link.remove();
}

$(document).ready(function () {

    $('#btn-upload-definitions').click(function () {

        try {
            var strDefinitions = $('.modal-json-textarea').val();

            var jsonDefinitions = JSON.parse(strDefinitions);

            $.ajax({
                url: 'MonitorSeries/ImportDefinitions',
                type: 'POST',
                data: JSON.stringify(jsonDefinitions),
                contentType: 'application/json',
                success: function (data) {
                    location.reload();
                },
                error: function (err) {
                    console.error(err);
                    alert(err.responseJSON.Error);
                }
            });

        } catch (err) {
            console.error(err);
            alert('Something went wrong, please try again');
        }
    });

});