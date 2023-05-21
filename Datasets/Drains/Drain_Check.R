# CBD Data Reads
catch <- read.csv('Test.csv')
catch = catch[c(1,2)]

in_out = read.csv('cbd_in_out.csv')
in_out <- in_out[c(1,2)]
man_hole <- read.csv('cbd_manhole.csv')
man_hole <- man_hole[c(1,2)]

plot(x = man_hole$X, y = man_hole$Y, col=4, pch='.')
points(x = catch$X, y = catch$Y, col=2, pch='.')
points(x = in_out$X, y = in_out$Y, col=3, pch='.')

# Orakei Data Reads 
o_catch <- read.csv('orakei_raw_catch.csv')
o_io <- read.csv('orakei_raw_in_out.csv')
o_man <- read.csv('orakei_raw_man.csv')

o_catch = o_catch[c(1,2)]
o_io = o_io[c(1,2)]
o_man = o_man[c(1,2)]

plot(x = o_catch$X, y = o_catch$Y, col=4, pch='.')
points(x = o_io$X, y = o_io$Y, col=2, pch='.')
points(x = o_man$X, y = o_man$Y, col=3, pch='.')


# Penrose Data Reads 
p_catch <- read.csv('penrose_raw_catch.csv')
p_io <- read.csv('penrose_raw_in_out.csv')
p_man <- read.csv('penrose_raw_man.csv')

p_catch = p_catch[c(1,2)]
p_io = p_io[c(1,2)]
p_man = p_man[c(1,2)]

plot(x = p_catch$X, y = p_catch$Y, col=4, pch='.')
points(x = p_io$X, y = p_io$Y, col=2, pch='.')
points(x = p_man$X, y = p_man$Y, col=3, pch='.')



# CBD Writes 
write.csv(catch, paste(getwd(), '/cbd_catch.csv', sep = ""), row.names = FALSE)
write.csv(in_out, paste(getwd(), '/cbd_in_out.csv', sep = ""), row.names = FALSE)
write.csv(man_hole, paste(getwd(), '/cbd_man_hole.csv', sep = ""), row.names = FALSE)

# Orakei Writes 
write.csv(o_catch, paste(getwd(), '/orakei_catch.csv', sep = ""), row.names = FALSE)
write.csv(o_io, paste(getwd(), '/orakei_in_out.csv', sep = ""), row.names = FALSE)
write.csv(o_man, paste(getwd(), '/orakei_man_hole.csv', sep = ""), row.names = FALSE)

# Penrose Writes 
write.csv(p_catch, paste(getwd(), '/penrose_catch.csv', sep = ""), row.names = FALSE)
write.csv(p_io, paste(getwd(), '/penrose_in_out.csv', sep = ""), row.names = FALSE)
write.csv(p_man, paste(getwd(), '/penrose_man_hole.csv', sep = ""), row.names = FALSE)

#TopLeft
#Lat, Lng: -36.835587, 174.741887
#TopRight
#Lat, Lng: -36.835048, 174.779712
#BottomLeft
#Lat, Lng: -36.869793, 174.742668
#BottomRight 
#Lat, Lng: -36.869240, 174.780523

c1 <- read.csv('cbd_catch.csv')
c2 <- read.csv('cbd_in_out.csv')
c3 <- read.csv('cbd_manhole.csv')
plot(x = c1$X, y = c1$Y, col=4, pch=',')
points(x = c2$X, y = c2$Y, col=2, pch=',')
points(x = c3$X, y = c3$Y, col=3, pch=',')
