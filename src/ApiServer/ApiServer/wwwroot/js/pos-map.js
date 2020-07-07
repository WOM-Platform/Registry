var map;
var marker = false;

function initPosMap() {
    map = new google.maps.Map(
        document.getElementById('posMap'),
        {
            center: new google.maps.LatLng(45.788, 9.948),
            zoom: 4,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        }
    );

    google.maps.event.addListener(map, 'click', function(event) {
        var clickedLocation = event.latLng;
        if(marker === false){
            marker = new google.maps.Marker({
                position: clickedLocation,
                map: map,
                draggable: true
            });
            google.maps.event.addListener(marker, 'dragend', function(event){
                markerLocation();
            });
        } else {
            marker.setPosition(clickedLocation);
        }
        markerLocation();
    });
}

function markerLocation(){
    var currentLocation = marker.getPosition();
    document.getElementById('mapLat').value = currentLocation.lat();
    document.getElementById('mapLng').value = currentLocation.lng();
}

google.maps.event.addDomListener(window, 'load', initPosMap);
