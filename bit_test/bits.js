const DEFAULT_BITS = 16;

function pb(x, numBits) {
    
    if (!numBits)
        numBits = DEFAULT_BITS;
    
    var str = '';
    var n = 0;
    for (var i = numBits - 1; i >= 0; --i) {
        
        if (n > 0) {
            if ((n % 8) == 0) {
                str += ' ';
            }
        }
        
        if ((x >> i) & 1)
            str += '1';
        else
            str += '0';
            
        ++n;
    }
    
    console.log(str);
}

var x = 1337;
var y = (x >> 8) & 0xff | ((x & 0xff) << 8);

pb(x);
pb(y);