
function downloadFile(uri, fileName) {
    var link = document.createElement("a");
    link.setAttribute('download', fileName);
    link.href = uri;
    document.body.appendChild(link);
    link.click();
    link.remove();
}

function deleteSeriesData(itemId, groupValue) {
    if (confirm('Clear series data?')) {
        fetch(`/MonitorSeries/DeleteData?id=${itemId}&groupValue=${groupValue}`, {
            method: 'delete',
            headers: {
                'Content-Type': 'application/json'
            },
        });
    }
}

$(document).ready(function () {

    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
      return new bootstrap.Popover(popoverTriggerEl)
    });

    $(".checkbox.readonly").click(function () { return false; });

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