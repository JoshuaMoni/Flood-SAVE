catch <- read.csv('Test.csv')
catch = catch[c(1,2)]
#catch <- subset(catch, catch$X > -36.835587 and catch$X > -36.835587)
in_out = read.csv('cbd_in_out.csv')
in_out <- in_out[c(1,2)]
man_hole <- read.csv('cbd_manhole.csv')
man_hole <- man_hole[c(1,2)]

plot(x = man_hole$X, y = man_hole$Y, col=4, pch='.')
points(x = catch$X, y = catch$Y, col=2, pch='.')
points(x = in_out$X, y = in_out$Y, col=3, pch='.')


write.csv(catch, paste(getwd(), '/cbd_catch.csv', sep = ""), row.names = FALSE)
write.csv(in_out, paste(getwd(), '/cbd_in_out.csv', sep = ""), row.names = FALSE)
write.csv(man_hole, paste(getwd(), '/cbd_man_hole.csv', sep = ""), row.names = FALSE)

#TopLeft
#Lat, Lng: -36.835587, 174.741887
#TopRight
#Lat, Lng: -36.835048, 174.779712
#BottomLeft
#Lat, Lng: -36.869793, 174.742668
#BottomRight 
#Lat, Lng: -36.869240, 174.780523
