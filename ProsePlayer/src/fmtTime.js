export default function (secVal, withFrac, padMinutes) {
  var mins = Math.floor(secVal / 60);
  var restSec = secVal - mins * 60;
  var sec = Math.floor(restSec);
  var frac = (restSec - sec) + "";
  frac = frac.substr(2) + "00";
  frac = "." + frac.substr(0, 2);
  if (!withFrac) frac = "";
  var p1val = padMinutes ? 3 : 2;
  return (mins + ":").padStart(p1val, "0") + (sec + "").padStart(2, "0") + frac;
}

