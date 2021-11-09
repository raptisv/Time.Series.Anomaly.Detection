
function downloadFile(uri, fileName) {
    var link = document.createElement("a");
    link.setAttribute('download', fileName);
    link.href = uri;
    document.body.appendChild(link);
    link.click();
    link.remove();
}

function deleteSeriesData(itemId) {
    if (confirm('Clear series data?')) {
        fetch(`/MonitorSeries/DeleteData?id=${itemId}`, {
            method: 'delete',
            headers: {
                'Content-Type': 'application/json'
            },
        })
            .then(data => alert('Data cleared'));
    }
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